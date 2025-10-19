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
    private int currentJumps;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool facingRight = true;
    private bool isMyTurn = false;
    private Vector2 moveInput;

    private readonly NetworkVariable<bool> netFacingRight = new(
        true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ===========================================================
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
            Debug.Log($"[PlayerMovement:{name}] OnNetworkSpawn (Owner={OwnerClientId})");
            EnableInputs();
            currentJumps = maxJumps;
        }
        else
        {
            DisableInputs(); // Evita input em jogadores remotos
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsOwner)
            DisableInputs();
    }

    // ===========================================================
    private void EnableInputs()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
            Debug.Log($"[PlayerMovement:{name}] Move Action habilitada: {moveAction.action.name}");
        }
        else Debug.LogWarning($"[PlayerMovement:{name}] Move Action não atribuída!");

        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
            Debug.Log($"[PlayerMovement:{name}] Jump Action habilitada: {jumpAction.action.name}");
        }
        else Debug.LogWarning($"[PlayerMovement:{name}] Jump Action não atribuída!");
    }

    private void DisableInputs()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJump;
        }
    }

    // ===========================================================
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

    // ===========================================================
    private void ReadMovementInput()
    {
        moveInput = Vector2.zero;

        // Usa teclas para movimento lateral
        if (Keyboard.current == null) return;

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            moveInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            moveInput.x += 1f;
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (moveInput.x > 0 && !facingRight)
            SetFacingServerRpc(true);
        else if (moveInput.x < 0 && facingRight)
            SetFacingServerRpc(false);
    }

    // ===========================================================
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
        netAnimator.SetTrigger("Jump"); // <- Corrigido (parâmetro igual ao Animator)
    }

    // ===========================================================
    private void CheckGround()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (IsOwner && isGrounded && !wasGrounded)
        {
            Debug.Log($"[PlayerMovement:{name}] Tocou o solo — resetando pulos.");
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

    // ===========================================================
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
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}


