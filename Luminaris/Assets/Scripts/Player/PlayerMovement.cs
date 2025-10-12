using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

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
    [SerializeField] private NetworkAnimator netAnimator;

    //[Header("Input Actions")]
    //[SerializeField] private InputActionReference moveAction;
    //[SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private Vector2 groundCheckSize = new(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;


    private bool isGrounded;
    private bool isMoving;
    private bool jumpPressed;

    private AudioSource walkAudio;
    private bool isActiveTurn = false;
    private float moveInput;
    private bool isFacingRight = true;


    private NetworkVariable<int> jumpsRemaining = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (netAnimator == null) netAnimator = GetComponent<NetworkAnimator>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerMovement] Spawned — OwnerClientId={OwnerClientId}, IsOwner={IsOwner}");

        if (IsServer)
        {
            Debug.Log($"[PlayerMovement] (Server) Registrando {OwnerClientId} no TurnControl.");
            TurnControl.Instance?.RegisterPlayer(this);
        }

        // Atualiza HUD em todos os clientes
        jumpsRemaining.OnValueChanged += (_, _) => playerUI?.OnJumpsChanged?.Invoke();
    }

    private void Update()
    {
        if (!IsOwner || !isActiveTurn) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isActiveTurn) return;
        Move();
    }

    private void Move()
    {
        float moveVelocity = moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(moveVelocity, rb.linearVelocity.y);

        if (moveVelocity != 0)
            spriteRenderer.flipX = moveVelocity < 0;

        anim.SetFloat("Speed", Mathf.Abs(moveVelocity));
        Debug.Log($"[PlayerMovement] (Client {OwnerClientId}) Movendo: {moveVelocity}");
    }

    private void TryJump()
    {
        if (!IsGrounded()) return;
        if (jumpsRemaining.Value <= 0) return;

        jumpsRemaining.Value--;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        netAnimator.SetTrigger("Jump"); // sincroniza animação via rede

        Debug.Log($"[PlayerMovement] (Client {OwnerClientId}) Pulou! Restam {jumpsRemaining.Value}");
    }

    private bool IsGrounded()
    {
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
        isGrounded = hit != null;
        return isGrounded;
    }

    // =====================================================
    // === CONTROLE DE TURNOS (SINCRONIZADO PELO SERVIDOR) =
    // =====================================================
    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        isActiveTurn = active;

        if (active)
        {
            jumpsRemaining.Value = 3; // reseta os pulos no início do turno
            playerUI?.StartTurn();
        }
        else
        {
            playerUI?.EndTurn();
        }

        string status = active ? "ATIVO" : "INATIVO";
        Debug.Log($"[PlayerMovement] Jogador {OwnerClientId} agora está {status}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, 0.15f);
    }
#endif
}
