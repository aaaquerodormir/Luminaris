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
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private NetworkAnimator netAnimator;
    [SerializeField] private PlayerMovementUI playerMovementUI;

    [Header("Jump Physics")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float jumpHangGravityMultiplier = 0.5f;
    [SerializeField] private float jumpHangThreshold = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Ground Check")]
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    private float lastPressedJumpTime;
    private float coyoteTimeCounter;
    private bool isJumpCut;

    [Header("Controle de Pulo")]
    public readonly NetworkVariable<int> CompletedJumpsNet = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> MaxJumpsNet = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<int> JumpBuffTurnsLeft = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int server_extraJumpsApplied = 0;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = true;
    private Vector2 moveInput;
    private AudioSource walkAudio;
    private MovingPlatform currentPlatform;
    private float moveX;

    private readonly NetworkVariable<bool> netFacingRight = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        rb.gravityScale = gravityScale;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        facingRight = !netFacingRight.Value;
        UpdateFacingDirection();

        if (IsOwner)
        {
            EnableInputs();
            if (AudioManager.Instance != null)
            {
                walkAudio = AudioManager.Instance.PlayLoop("Andando", gameObject);
                if (walkAudio != null) walkAudio.Stop();
            }
        }
        else
        {
            DisableInputs();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner) DisableInputs();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();
    }

    private void EnableInputs()
    {
        moveAction?.action.Enable();
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJumpPressed;
            jumpAction.action.canceled += OnJumpReleased;
        }
    }

    private void DisableInputs()
    {
        moveAction?.action.Disable();
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJumpPressed;
            jumpAction.action.canceled -= OnJumpReleased;
        }
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;
        lastPressedJumpTime = jumpBufferTime;
    }

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;
        if (rb.linearVelocity.y > 0) isJumpCut = true;
    }

    private void Update()
    {
        CheckGround();
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        float yVel = rb.linearVelocity.y;
        UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);

        if (!IsOwner || !isMyTurn)
        {
            lastPressedJumpTime = 0;
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();

        if (walkAudio != null)
        {
            bool isMovingOnGround = isMoving && isGrounded;
            if (isMovingOnGround && !walkAudio.isPlaying) walkAudio.Play();
            else if (!isMovingOnGround && walkAudio.isPlaying) walkAudio.Stop();
        }

        lastPressedJumpTime -= Time.deltaTime;
        bool hasJumpsLeft = CompletedJumpsNet.Value < MaxJumpsNet.Value;

        if (lastPressedJumpTime > 0 && coyoteTimeCounter > 0f && hasJumpsLeft)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (currentPlatform != null)
        {
            Vector2 platformVelocity = currentPlatform.currentVelocity;
            rb.position += platformVelocity * Time.fixedDeltaTime;
        }

        if (!IsOwner)
        {
            rb.gravityScale = gravityScale;
            return;
        }

        if (!isMyTurn)
        {
            HandleMovement(isMyTurn);
        }
        else
        {
            HandleMovement(isMyTurn);
        }

        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold)
        {
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        }
        else if (isJumpCut)
        {
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void HandleMovement(bool isMyTurn)
    {
        if (!isMyTurn)
        {
            moveX = 0;
        }
        else
        {
            moveX = moveInput.x;
        }
        Vector2 finalVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        rb.linearVelocity = finalVelocity;

        if (moveX > 0 && !facingRight) SetFacingServerRpc(true);
        else if (moveX < 0 && facingRight) SetFacingServerRpc(false);
    }

    private void Jump()
    {
        if (!IsOwner) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastPressedJumpTime = 0;
        coyoteTimeCounter = 0f;
        isJumpCut = false;

        netAnimator.SetTrigger("Jump");
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("Pulando");

        SubmitJumpServerRpc();
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isGrounded && !wasGrounded)
        {
            if (isMyTurn && CompletedJumpsNet.Value >= MaxJumpsNet.Value)
            {
                RequestEndTurn();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitJumpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId && !IsServer) return;

        if (CompletedJumpsNet.Value < MaxJumpsNet.Value)
        {
            CompletedJumpsNet.Value++;
            playerMovementUI.UpdateJumps(MaxJumpsNet.Value, CompletedJumpsNet.Value);
        }
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
        if (spriteRenderer != null) spriteRenderer.flipX = facingRight;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetFacingServerRpc(bool right)
    {
        if (netFacingRight.Value != right) netFacingRight.Value = right;
    }

    private void RequestEndTurn()
    {
        if (IsServer) TurnControl.Instance?.EndTurn();
        else SubmitEndTurnServerRpc();
    }

    [ServerRpc]
    private void SubmitEndTurnServerRpc()
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
        if (active && IsServer)
        {
            CompletedJumpsNet.Value = 0;
            playerMovementUI.UpdateJumps(MaxJumpsNet.Value, CompletedJumpsNet.Value);
        }

        if (!IsOwner) return;

        if (!active)
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            if (walkAudio != null && walkAudio.isPlaying) walkAudio.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlatformTouchServerRpc(NetworkObjectReference platformNetObj)
    {
        if (platformNetObj.TryGet(out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PlataformaInstavel platform))
            {
                platform.ActivateFallFromServer();
            }
        }
    }

    public void ApplyJumpPowerUp(int extraJumps, int durationTurns)
    {
        if (!IsServer) return;
        MaxJumpsNet.Value += extraJumps;
        server_extraJumpsApplied += extraJumps;
        JumpBuffTurnsLeft.Value = Mathf.Max(JumpBuffTurnsLeft.Value, durationTurns);
        playerMovementUI.UpdateJumps(MaxJumpsNet.Value, CompletedJumpsNet.Value);
    }

    public void DecrementBuffTurns()
    {
        if (!IsServer || JumpBuffTurnsLeft.Value == 0) return;
        JumpBuffTurnsLeft.Value--;
        if (JumpBuffTurnsLeft.Value <= 0)
        {
            RevertJumpPowerUp();
        }
    }

    private void RevertJumpPowerUp()
    {
        if (!IsServer) return;
        MaxJumpsNet.Value -= server_extraJumpsApplied;
        server_extraJumpsApplied = 0;
        JumpBuffTurnsLeft.Value = 0;
        playerMovementUI.UpdateJumps(MaxJumpsNet.Value, CompletedJumpsNet.Value);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out MovingPlatform movingPlatform))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f)
                {
                    currentPlatform = movingPlatform;
                    break;
                }
            }
        }

        if (IsOwner && collision.gameObject.TryGetComponent(out PlataformaInstavel platform))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f && platform.NetworkObject.IsSpawned)
                {
                    NotifyPlatformTouchServerRpc(platform.NetworkObject);
                    break;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.TryGetComponent(out MovingPlatform movingPlatform))
        {
            if (currentPlatform == movingPlatform)
            {
                currentPlatform = null;
            }
        }
    }
}