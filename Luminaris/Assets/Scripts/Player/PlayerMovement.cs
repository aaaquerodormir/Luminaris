using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
   // [SerializeField] private NetworkAnimator netAnimator;


    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    //[SerializeField] private Transform groundCheck;
    //[SerializeField] private Vector2 groundCheckSize = new(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Controle de Pulo")]
    [SerializeField] private int maxJumps = 3;
    private int currentJumps;

    private bool isGrounded;
    private bool isMyTurn;
    private bool facingRight = true;

    private Vector2 moveInput;

    private NetworkVariable<bool> netFacingRight = new NetworkVariable<bool>(
       true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
   );

    // ==============================
    // == INITIALIZATION ============
    // ==============================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!playerUI) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        // Espera o TurnControl existir antes de registrar
        if (IsServer)
            StartCoroutine(WaitForTurnControlAndRegister());

        // Só o dono local habilita inputs
        if (IsOwner)
        {
            // Se não estiver atribuída no inspector, tenta carregar dinamicamente
            if (moveAction == null || jumpAction == null)
                AssignActionsByOwner();

            moveAction?.action.Enable();
            jumpAction?.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void OnDestroy()
    {
        if (IsOwner && jumpAction != null)
            jumpAction.action.performed -= OnJump;
    }

    // ==============================
    // == REGISTRO DE TURNO ========
    // ==============================
    private IEnumerator WaitForTurnControlAndRegister()
    {
        while (TurnControl.Instance == null)
            yield return null;

        TurnControl.Instance.RegisterPlayer(this);
        Debug.Log($"[PlayerMovement] Registrado no TurnControl: {name}");
    }

    // ==============================
    // == INPUT SYSTEM ==============
    // ==============================
    private void AssignActionsByOwner()
    {
        string basePath = "InputActions/";
        if (OwnerClientId == 0)
        {
            moveAction = Resources.Load<InputActionReference>(basePath + "MovePlayer1");
            jumpAction = Resources.Load<InputActionReference>(basePath + "JumpPlayer1");
        }
        else
        {
            moveAction = Resources.Load<InputActionReference>(basePath + "MovePlayer2");
            jumpAction = Resources.Load<InputActionReference>(basePath + "JumpPlayer2");
        }

        Debug.Log($"[PlayerMovement] Atribuiu ações automaticamente para o jogador {OwnerClientId}.");
    }

    private void Update()
    {
        if (!IsOwner || !isMyTurn || moveAction == null) return;

        moveInput = moveAction.action.ReadValue<Vector2>();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;

        CheckGround();
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

        if (currentJumps <= 0)
            NotifyEndTurnServerRpc();
    }

    [ServerRpc]
    private void NotifyEndTurnServerRpc()
    {
        StartCoroutine(EndTurnAfterDelay());
    }

    private IEnumerator EndTurnAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        TurnControl.Instance.EndTurn();
    }

    // ==============================
    // == GROUND & ANIMATIONS ======
    // ==============================
    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetBool("isIdle", Mathf.Abs(moveInput.x) < 0.1f && isGrounded);
        anim.SetBool("isWalking", Mathf.Abs(moveInput.x) > 0.1f && isGrounded);
        anim.SetBool("isJumping", !isGrounded);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void UpdateFacingDirection()
    {
        facingRight = netFacingRight.Value;
        if (spriteRenderer != null)
            spriteRenderer.flipX = facingRight; // flip ajustado para sprite base virado à esquerda
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
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            playerUI?.EndTurn();
        }

        Debug.Log($"[PlayerMovement] Turno de {OwnerClientId}: {(active ? "Ativo" : "Inativo")}");
    }
}

