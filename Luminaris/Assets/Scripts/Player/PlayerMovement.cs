using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkRigidbody2D))]

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
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
    [SerializeField] private int maxJumps = 3;
    private int usedJumps = 0;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = true;
    private Vector2 moveInput;

    private readonly NetworkVariable<bool> netFacingRight =
        new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ==================================================
    // =============== INITIALIZATION ===================
    // ==================================================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!playerUI) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        if (IsOwner)
        {
            Debug.Log($"[PlayerMovement:{name}] OnNetworkSpawn — sou o dono (Owner={OwnerClientId})");
            EnableInputs();
        }
        else
        {
            DisableInputs();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner)
            DisableInputs();
    }

    // ==================================================
    // ================= INPUT CONTROL ==================
    // ==================================================
    private void EnableInputs()
    {
        moveAction?.action.Enable();
        jumpAction?.action.Enable();
        jumpAction.action.performed += OnJump;

        Debug.Log($"[PlayerMovement:{name}] Inputs habilitados — Move={moveAction?.name}, Jump={jumpAction?.name}");
    }

    private void DisableInputs()
    {
        moveAction?.action.Disable();
        jumpAction?.action.Disable();
        jumpAction.action.performed -= OnJump;
    }

    private void Update()
    {
        if (!IsOwner || !isMyTurn) return;

        moveInput = moveAction.action.ReadValue<Vector2>();
        CheckGround();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;

        Move();
    }

    // ==================================================
    // ================= MOVEMENT =======================
    // ==================================================
    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        // Corrige moonwalking
        if (moveInput.x > 0 && !facingRight)
            SetFacingServerRpc(true);
        else if (moveInput.x < 0 && facingRight)
            SetFacingServerRpc(false);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;
        TryJump();
    }

    private void TryJump()
    {
        if (!isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        usedJumps++;

        Debug.Log($"[PlayerMovement:{name}] Pulou! ({usedJumps}/{maxJumps})");
        netAnimator.SetTrigger("Jump");
        playerUI?.UpdateJumps(maxJumps - usedJumps);
    }

    // ==================================================
    // ================= GROUND CHECK ===================
    // ==================================================
    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            Debug.Log($"[PlayerMovement:{name}] Tocou o chão! Pulos usados: {usedJumps}");

            if (usedJumps >= maxJumps)
            {
                usedJumps = 0;
                Debug.Log($"[PlayerMovement:{name}] 🔸 Máximo de pulos atingido, encerrando turno!");
                RequestEndTurn();
            }
            else
            {
                playerUI?.UpdateJumps(maxJumps - usedJumps);
            }
        }
    }

    // ==================================================
    // ================= ANIMATIONS =====================
    // ==================================================
    private void UpdateAnimations()
    {
        Animator a = netAnimator.Animator;
        if (a == null) return;

        a.SetBool("IsGrounded", isGrounded);
        a.SetBool("IsMoving", Mathf.Abs(moveInput.x) > 0.1f);
        a.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    // ==================================================
    // ================ NETWORK SYNC ====================
    // ==================================================
    private void UpdateFacingDirection()
    {
        facingRight = netFacingRight.Value;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    [ServerRpc]
    private void SetFacingServerRpc(bool right)
    {
        netFacingRight.Value = right;
    }

    // ==================================================
    // ================= TURN SYSTEM ====================
    // ==================================================
    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        Debug.Log($"[TurnControl][ServerRpc] Ativando turno de {name} -> Active={active}");
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        Debug.Log($"[TurnControl][ClientRpc] Player={name} Active={active}");
        SetTurnActive(active);
    }

    private void SetTurnActive(bool active)
    {
        isMyTurn = active;

        if (!IsOwner) return;

        if (active)
        {
            usedJumps = 0;
            playerUI?.StartTurn(maxJumps);
            Debug.Log($"[PlayerMovement:{name}] 🔹 TURNO ATIVO");
        }
        else
        {
            playerUI?.EndTurn();
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            Debug.Log($"[PlayerMovement:{name}] 🔸 TURNO ENCERRADO");
        }
    }

    private void RequestEndTurn()
    {
        if (IsServer)
        {
            TurnControl.Instance?.EndTurn();
        }
        else
        {
            SubmitEndTurnServerRpc();
        }
    }

    [ServerRpc]
    private void SubmitEndTurnServerRpc()
    {
        TurnControl.Instance?.EndTurn();
    }

    // ==================================================
    // ================= DEBUG VISUAL ===================
    // ==================================================
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}


