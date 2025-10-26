using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkRigidbody2D))]

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    //[SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private NetworkAnimator netAnimator;


    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;


    [Header("Ground Check")]
    //[SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Controle de Pulo")]
    [SerializeField] private int baseMaxJumps = 3;
    public readonly NetworkVariable<int> CompletedJumpsNet = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private bool pendingJump = false;
    public event System.Action<ulong, int> OnTurnStarted;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = false;
    private Vector2 moveInput;

    // Networked flip
    private readonly NetworkVariable<bool> netFacingRight =
        new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ===== PowerUp state (networked summaries) =====
    // Total extra jumps currently active (sum of active jump buffs)
    private NetworkVariable<int> jumpBonusNet = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // Total movement multiplier (product of active speed buffs). Start = 1.0f
    private NetworkVariable<float> speedMultiplierNet = new(
        1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // ===== Server-only: list of active buffs with their remaining turns =====
    private class ActiveJumpBuff { public int extraJumps; public int turnsLeft; }
    private class ActiveSpeedBuff { public float multiplier; public int turnsLeft; }

    // Only the server modifies/reads these lists. Clients read only via NetworkVariables above.
    private readonly List<ActiveJumpBuff> activeJumpBuffs = new();
    private readonly List<ActiveSpeedBuff> activeSpeedBuffs = new();

    // ================== Unity / Netcode lifecycle ==================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();
        CompletedJumpsNet.OnValueChanged += HandleJumpsChanged;
        jumpBonusNet.OnValueChanged += (_, __) => { /* optional UI */ };
        speedMultiplierNet.OnValueChanged += (_, __) => { /* optional UI */ };

        if (IsOwner) EnableInputs();
        else DisableInputs();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();
        CompletedJumpsNet.OnValueChanged -= HandleJumpsChanged;
    }

    private void HandleJumpsChanged(int oldVal, int newVal)
    {
        int remaining = GetMaxJumps() - newVal;
        UpdateJumpsClientRpc(remaining);
    }

    [ClientRpc]
    private void UpdateJumpsClientRpc(int remainingJumps)
    {
        JumpHUD.NotifyJumpsChanged(OwnerClientId, remainingJumps);
    }

    // ================== Input ==================
    private void EnableInputs()
    {
        moveAction?.action.Enable();
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void DisableInputs()
    {
        moveAction?.action.Disable();
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJump;
        }
    }

    // ================== Update/FixedUpdate ==================
    private void Update()
    {
        CheckGround();

        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        float yVel = rb.linearVelocity.y;
        UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);

        if (!IsOwner || !isMyTurn) return;
        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveX = moveInput.x;
        float speed = baseMoveSpeed * speedMultiplierNet.Value;
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);

        if (moveX > 0 && !facingRight)
            SetFacingServerRpc(true);
        else if (moveX < 0 && facingRight)
            SetFacingServerRpc(false);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn || !isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        pendingJump = true;
        netAnimator.SetTrigger("Jump");
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            if (pendingJump)
            {
                pendingJump = false;
                if (IsServer)
                    CompletedJumpsNet.Value++;
                else
                    SubmitJumpServerRpc();
            }

            if (isMyTurn && CompletedJumpsNet.Value >= GetMaxJumps())
            {
                RequestEndTurn();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitJumpServerRpc(ServerRpcParams rpcParams = default)
    {
        // Only server increments, but RPC is requested by owner
        CompletedJumpsNet.Value++;
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateAnimatorServerRpc(bool moving, bool grounded, float yVel, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        Animator a = netAnimator.Animator;
        if (a == null) return;
        a.SetBool("IsMoving", moving);
        a.SetBool("IsGrounded", grounded);
        a.SetFloat("yVelocity", yVel);
    }

    private void UpdateFacingDirection()
    {
        bool newFacing = netFacingRight.Value;
        if (newFacing == facingRight) return;
        facingRight = newFacing;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetFacingServerRpc(bool right)
    {
        if (netFacingRight.Value != right)
            netFacingRight.Value = right;
    }

    // =============== Turn control helpers =================
    public void RequestEndTurn()
    {
        if (IsServer)
            TurnControl.Instance?.EndTurn();
        else
            SubmitEndTurnServerRpc();
    }

    [ServerRpc]
    private void SubmitEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        TurnControl.Instance?.EndTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        SetTurnActive(active);
    }

    private void SetTurnActive(bool active)
    {
        isMyTurn = active;
        if (!IsOwner) return;

        if (active)
        {
            if (IsServer)
            {
                CompletedJumpsNet.Value = 0;
            }
            // exemplo: notificar HUD
            // OnTurnStarted?.Invoke(OwnerClientId, GetMaxJumps());
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Called by server when a player's turn ends (TurnControl will call this)
    public void OnTurnEndedServer()
    {
        if (!IsServer) return;
        DecrementBuffTurns();
    }

    // ================== PowerUp methods (Server only) ==================
    public void AddJumpPowerup_Server(int extraJumps, int durationTurns)
    {
        if (!IsServer) return;
        if (extraJumps <= 0 || durationTurns <= 0) return;

        activeJumpBuffs.Add(new ActiveJumpBuff { extraJumps = extraJumps, turnsLeft = durationTurns });
        RecalculateJumpBonusNet();
    }

    public void AddSpeedPowerup_Server(float multiplier, int durationTurns)
    {
        if (!IsServer) return;
        if (multiplier <= 0f || durationTurns <= 0) return;

        activeSpeedBuffs.Add(new ActiveSpeedBuff { multiplier = multiplier, turnsLeft = durationTurns });
        RecalculateSpeedMultiplierNet();
    }

    private void DecrementBuffTurns()
    {
        // jump buffs
        for (int i = activeJumpBuffs.Count - 1; i >= 0; i--)
        {
            activeJumpBuffs[i].turnsLeft--;
            if (activeJumpBuffs[i].turnsLeft <= 0)
                activeJumpBuffs.RemoveAt(i);
        }
        RecalculateJumpBonusNet();

        // speed buffs
        for (int i = activeSpeedBuffs.Count - 1; i >= 0; i--)
        {
            activeSpeedBuffs[i].turnsLeft--;
            if (activeSpeedBuffs[i].turnsLeft <= 0)
                activeSpeedBuffs.RemoveAt(i);
        }
        RecalculateSpeedMultiplierNet();
    }

    private void RecalculateJumpBonusNet()
    {
        int total = 0;
        foreach (var b in activeJumpBuffs) total += b.extraJumps;
        jumpBonusNet.Value = total;
    }

    private void RecalculateSpeedMultiplierNet()
    {
        float total = 1f;
        foreach (var b in activeSpeedBuffs) total *= b.multiplier;
        speedMultiplierNet.Value = total;
    }

    // =============== Helpers ====================
    // Max jumps = base + bonus (from networked summary)
    public int GetMaxJumps()
    {
        return baseMaxJumps + jumpBonusNet.Value;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    // Platform notification (mantive)
    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlatformTouchServerRpc(NetworkObjectReference platformNetObj, ServerRpcParams rpcParams = default)
    {
        if (platformNetObj.TryGet(out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PlataformaInstavel platform))
            {
                platform.ActivateFallFromServer();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsOwner) return;

        if (collision.gameObject.TryGetComponent(out PlataformaInstavel platform))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    if (platform.NetworkObject.IsSpawned)
                    {
                        NotifyPlatformTouchServerRpc(platform.NetworkObject);
                    }
                    break;
                }
            }
        }
    }
}

