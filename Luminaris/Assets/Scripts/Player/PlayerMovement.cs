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
    private int remainingJumps;
    private bool pendingJump = false;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = false;
    private Vector2 moveInput;

    // sincroniza flip entre host/client
    private readonly NetworkVariable<bool> netFacingRight =
       new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ==============================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        if (!playerUI) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
            EnableInputs();
        else
            DisableInputs();

        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        if (IsServer)
        {
            remainingJumps = maxJumps;
            playerUI?.SetJumps(remainingJumps);
        }

        Debug.Log($"[PlayerMovement:{name}] Spawned | Owner={OwnerClientId} | Server={IsServer}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner) DisableInputs();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();
    }

    // ==============================
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

    // ==============================
    private void Update()
    {
        CheckGround();
        // 🔹 Agora sincronizamos o estado da animação via RPC
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        float yVel = rb.linearVelocity.y;
        UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);

        if (!IsOwner || !isMyTurn)
            return;

        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        //// 🔹 Agora sincronizamos o estado da animação via RPC
        //bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        //float yVel = rb.linearVelocity.y;
        //UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;
        Move();
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !facingRight)
            SetFacingServerRpc(true);
        else if (moveInput.x < 0 && facingRight)
            SetFacingServerRpc(false);
    }

    // =======================================
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;
        if (!isGrounded || remainingJumps <= 0) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        netAnimator.SetTrigger("Jump");
        pendingJump = true;

        remainingJumps--;
        playerUI?.SetJumps(remainingJumps);

        Debug.Log($"[PlayerMovement:{name}] Pulou → restam {remainingJumps} pulos");
    }

    // =======================================
    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded && pendingJump)
        {
            pendingJump = false;
            Debug.Log($"[PlayerMovement:{name}] Aterrissou");

            if (remainingJumps <= 0)
            {
                Debug.Log($"[PlayerMovement:{name}] 🚩 Acabaram os pulos");
                RequestEndTurn();
            }
        }
    }

    // =======================================
    [ServerRpc(RequireOwnership = false)]
    private void UpdateAnimatorServerRpc(bool moving, bool grounded, float yVel)
    {
        if (netAnimator.Animator == null) return;
        var a = netAnimator.Animator;
        a.SetBool("IsMoving", moving);
        a.SetBool("IsGrounded", grounded);
        a.SetFloat("yVelocity", yVel);
    }

    private void UpdateFacingDirection()
    {
        bool newFacing = netFacingRight.Value;
        if (facingRight == newFacing) return;

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

    // =======================================
    private void RequestEndTurn()
    {
        if (IsServer)
        {
            Debug.Log($"[PlayerMovement:{name}] 🔁 Encerrando turno (Host)");
            TurnControl.Instance?.EndTurn();
        }
        else
        {
            Debug.Log($"[PlayerMovement:{name}] 🔁 Solicitando fim de turno via RPC");
            SubmitEndTurnServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitEndTurnServerRpc()
    {
        TurnControl.Instance?.EndTurn();
    }

    // =======================================
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
            remainingJumps = maxJumps;
            playerUI?.SetJumps(remainingJumps);
            Debug.Log($"[PlayerMovement:{name}] ▶️ Turno iniciado — {remainingJumps} pulos");
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            Debug.Log($"[PlayerMovement:{name}] ⏹ Turno encerrado");
        }
    }

    // =======================================
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}


