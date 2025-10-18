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
    //[SerializeField] private Vector2 groundCheckSize = new(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Controle de Pulo")]
    [SerializeField] private int maxJumps = 3;
    private int currentJumps;

    private bool isGrounded;
    private bool facingRight = true;
    private bool isMyTurn = false;
    private Vector2 moveInput;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveLeftAction;
    [SerializeField] private InputActionReference moveRightAction;
    [SerializeField] private InputActionReference jumpAction;

    private readonly NetworkVariable<bool> netFacingRight = new(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ==================================================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        if (IsOwner)
        {
            Debug.Log($"[PlayerMovement:{name}] OnNetworkSpawn — sou o dono ({OwnerClientId})");
            AssignInputActions();
            EnableInputs();
            currentJumps = maxJumps;
        }
        else
        {
            Debug.Log($"[PlayerMovement:{name}] OnNetworkSpawn — não sou o dono ({OwnerClientId})");
        }
    }

    private void OnDestroy()
    {
        if (IsOwner)
            DisableInputs();
    }

    // ================================================
    private void AssignInputActions()
    {
        // Carrega o arquivo principal (ex: "InputActions.inputactions")
        var inputAsset = Resources.Load<InputActionAsset>("InputActions/InputActions");
        if (inputAsset == null)
        {
            Debug.LogError($"[PlayerMovement:{name}] ERRO: Não encontrou InputActions em Resources/InputActions/InputActions!");
            return;
        }

        bool isPlayer1 = (OwnerClientId == 0);

        // Busca ações pelo nome exato dentro do Input Actions Editor
        moveLeftAction = InputActionReference.Create(inputAsset.FindAction(isPlayer1 ? "MovePlayer1" : "MovePlayer2"));
        jumpAction = InputActionReference.Create(inputAsset.FindAction(isPlayer1 ? "JumpPlayer1" : "JumpPlayer2"));

        if (moveLeftAction == null || jumpAction == null)
        {
            Debug.LogError($"[PlayerMovement:{name}] ERRO: Falhou ao encontrar ações de input!");
        }
        else
        {
            Debug.Log($"[PlayerMovement:{name}] Inputs carregados com sucesso para Player{(isPlayer1 ? 1 : 2)}.");
        }
    }

    private void EnableInputs()
    {
        moveLeftAction?.action.Enable();
        moveRightAction?.action.Enable();
        jumpAction?.action.Enable();
        jumpAction.action.performed += OnJump;
    }

    private void DisableInputs()
    {
        moveLeftAction?.action.Disable();
        moveRightAction?.action.Disable();
        jumpAction?.action.Disable();
        jumpAction.action.performed -= OnJump;
    }

    // ================================================
    private void Update()
    {
        if (!IsOwner || !isMyTurn) return;

        ReadMovementInput();
        CheckGround();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;
        Move();
    }

    private void ReadMovementInput()
    {
        moveInput = Vector2.zero;

        // Lê o valor das ações de movimento (botões)
        var move = moveLeftAction.action.ReadValue<Vector2>();

        // MAS se for Button, use IsPressed()
        if (moveLeftAction.action.IsPressed()) moveInput.x -= 1f;
        if (moveRightAction != null && moveRightAction.action.IsPressed()) moveInput.x += 1f;


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

        Debug.Log($"[PlayerMovement:{name}] Pulou! Restam {currentJumps} pulos.");
        netAnimator.SetTrigger("JumpTrigger");
    }

    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            Debug.Log($"[PlayerMovement:{name}] Tocou o solo — reiniciando pulos.");
            currentJumps = maxJumps;
        }
    }

    private void UpdateAnimations()
    {
        Animator a = netAnimator.Animator;
        if (a == null) return;

        a.SetBool("IsGrounded", isGrounded);
        a.SetBool("IsMoving", Mathf.Abs(moveInput.x) > 0.1f);
        a.SetFloat("yVelocity", rb.linearVelocity.y);
    }

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
    // ======== TURN CONTROL SYNC (Host + Client) ========
    // ==================================================
    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        Debug.Log($"[PlayerMovement:{name}] (ServerRpc) TurnActive={active}");
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        Debug.Log($"[PlayerMovement:{name}] (ClientRpc) TurnActive={active}");
        SetTurnActive(active);
    }

    private void SetTurnActive(bool active)
    {
        isMyTurn = active;

        if (!IsOwner) return;

        if (active)
        {
            currentJumps = maxJumps;
            Debug.Log($"[PlayerMovement:{name}] Turno ATIVO.");
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            Debug.Log($"[PlayerMovement:{name}] Turno ENCERRADO.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
    }
}


