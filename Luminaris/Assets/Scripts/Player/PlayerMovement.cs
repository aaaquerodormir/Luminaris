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


    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Controle de Pulo")]
    [SerializeField] private int maxJumps = 3;
    private int currentJumps;
    private bool isGrounded;
    private bool isMyTurn;
    private bool facingRight = true;
    private Vector2 moveInput;

    private BoxCollider2D groundCollider;

    private NetworkVariable<bool> netFacingRight = new NetworkVariable<bool>(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // ============================== //
    // == INITIALIZATION ============ //
    // ============================== //

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        if (!playerUI) playerUI = GetComponent<PlayerMovementUI>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (groundCheck)
            groundCollider = groundCheck.GetComponent<BoxCollider2D>();

        // 🔒 Garante que o groundLayer detecta a layer "Plataforma"
        int plataformaLayer = LayerMask.NameToLayer("Plataforma");
        if (plataformaLayer >= 0)
            groundLayer = 1 << plataformaLayer;
        else
            Debug.LogWarning("[PlayerMovement] Layer 'Plataforma' não encontrada. Configure no Inspector!");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        if (IsServer)
            StartCoroutine(RegisterToTurnControl());

        if (IsOwner)
        {
            if (moveAction == null || jumpAction == null)
                AssignInputActions();

            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            jumpAction.action.performed += OnJump;
        }

        Debug.Log($"[Network] Player {OwnerClientId} spawned | Authority = {(IsServer ? "Server" : "Owner")}");
    }

    private void OnDestroy()
    {
        if (IsOwner && jumpAction != null)
            jumpAction.action.performed -= OnJump;
    }

    private IEnumerator RegisterToTurnControl()
    {
        while (TurnControl.Instance == null) yield return null;
        TurnControl.Instance.RegisterPlayer(this);
    }

    private void AssignInputActions()
    {
        string basePath = "InputActions/";
        moveAction = Resources.Load<InputActionReference>(OwnerClientId == 0 ? basePath + "MovePlayer1" : basePath + "MovePlayer2");
        jumpAction = Resources.Load<InputActionReference>(OwnerClientId == 0 ? basePath + "JumpPlayer1" : basePath + "JumpPlayer2");
    }

    // ============================== //
    // == GAME LOOP ================= //
    // ============================== //

    private void Update()
    {
        if (!IsOwner || !isMyTurn) return;

        moveInput = moveAction.action.ReadValue<Vector2>();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        CheckGround();

        // 🔒 Se o turno acabou e o player está no chão, ele fica imóvel
        if (!isMyTurn && isGrounded)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (!IsOwner || !isMyTurn) return;

        Move();
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !facingRight) SetFacingServerRpc(true);
        else if (moveInput.x < 0 && facingRight) SetFacingServerRpc(false);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;
        TryJump();
    }

    private void TryJump()
    {
        if (!isGrounded || currentJumps <= 0) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        currentJumps--;
        playerUI?.UpdateJumps(currentJumps);

        anim.SetBool("isJumping", true);
        anim.SetBool("IsGrounded", false);

        Debug.Log($"[Jump] Player {OwnerClientId} pulou — jumps restantes: {currentJumps}");

        if (currentJumps <= 0)
            NotifyEndTurnServerRpc();
    }

    [ServerRpc]
    private void NotifyEndTurnServerRpc()
    {
        StartCoroutine(EndTurnAfterLanding());
    }

    private IEnumerator EndTurnAfterLanding()
    {
        // 🔄 Aguarda o personagem tocar o chão antes de encerrar o turno
        yield return new WaitUntil(() => isGrounded);
        yield return new WaitForSeconds(0.2f);

        Debug.Log($"[Turn] Player {OwnerClientId} finalizou turno (pousou).");
        TurnControl.Instance.EndTurn();
    }

    // ============================== //
    // == GROUND CHECK ============== //
    // ============================== //

    private void CheckGround()
    {
        if (groundCollider == null)
        {
            Debug.LogError("[GroundCheck] Nenhum BoxCollider2D encontrado no objeto groundCheck!");
            return;
        }

        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCollider.bounds.center, groundCollider.size, 0f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            anim.SetBool("IsGrounded", true);
            anim.SetBool("isJumping", false);
            Debug.Log($"[GroundCheck] Player {OwnerClientId} pousou na layer 'Plataforma'.");
        }
        else if (!isGrounded && wasGrounded)
        {
            anim.SetBool("IsGrounded", false);
            Debug.Log($"[GroundCheck] Player {OwnerClientId} saiu da plataforma.");
        }
    }

    // ============================== //
    // == ANIMATIONS ================ //
    // ============================== //

    private void UpdateAnimations()
    {
        Animator a = netAnimator.Animator;
        if (a == null) return;

        a.SetBool("isIdle", Mathf.Abs(moveInput.x) < 0.1f && isGrounded);
        a.SetBool("isWalking", Mathf.Abs(moveInput.x) > 0.1f && isGrounded);
        a.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void UpdateFacingDirection()
    {
        facingRight = netFacingRight.Value;
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingRight;
    }

    [ServerRpc]
    private void SetFacingServerRpc(bool right)
    {
        netFacingRight.Value = right;
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

        if (active)
        {
            currentJumps = maxJumps;
            playerUI?.StartTurn(maxJumps);
            Debug.Log($"[TurnControl] Player {OwnerClientId} iniciou turno.");
        }
        else
        {
            moveInput = Vector2.zero;
            playerUI?.EndTurn();
            Debug.Log($"[TurnControl] Player {OwnerClientId} encerrou turno.");
        }
    }

    // ============================== //
    // == DEBUG GIZMOS ============== //
    // ============================== //

    private void OnDrawGizmosSelected()
    {
        if (groundCollider == null)
        {
            if (groundCheck)
            {
                groundCollider = groundCheck.GetComponent<BoxCollider2D>();
                if (groundCollider == null) return;
            }
            else return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCollider.bounds.center, groundCollider.size);
    }
}
