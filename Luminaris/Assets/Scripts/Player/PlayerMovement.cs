using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
//[RequireComponent(typeof(NetworkTransform))]

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

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

    
    private bool isJumpPressed;
    private bool isFacingRight = true;
    private float horizontalInput;

    private AudioSource walkAudio;

    private bool isGrounded;
    private bool isActive = false; // 🔸 controlado pelo TurnControl
    private float moveInput;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerMovement] Spawned — OwnerClientId={OwnerClientId}, IsOwner={IsOwner}");

        if (IsServer)
        {
            Debug.Log($"[PlayerMovement] (Server) Registrando {OwnerClientId} no TurnControl.");
            TurnControl.Instance?.RegisterPlayer(this);
        }
    }

    private void Update()
    {
        if (!IsOwner || !isActive) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isActive) return;

        Move();
    }

    private void Move()
    {
        float moveVelocity = moveInput * moveSpeed;

        rb.linearVelocity = new Vector2(moveVelocity, rb.linearVelocity.y);

        if (moveVelocity != 0)
        {
            spriteRenderer.flipX = moveVelocity < 0;
        }

        anim.SetFloat("Speed", Mathf.Abs(moveVelocity));

        Debug.Log($"[PlayerMovement] (ClientID={OwnerClientId}) Movendo: {moveVelocity}");
    }

    private void TryJump()
    {
        if (!IsGrounded()) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        anim.SetTrigger("Jump");

        Debug.Log($"[PlayerMovement] (ClientID={OwnerClientId}) Pulou!");
    }

    private bool IsGrounded()
    {
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
        isGrounded = hit != null;
        return isGrounded;
    }

    // ==================================================
    // ============ CONTROLE DE TURNOS ==================
    // ==================================================
    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        isActive = active;

        string status = active ? "ATIVO" : "INATIVO";
        Debug.Log($"[PlayerMovement] Player {OwnerClientId} agora está {status} (éOwner={IsOwner})");

        if (!active)
        {
            anim.SetFloat("Speed", 0);
        }
    }

    // ==================================================
    // ============ AUXÍLIO DE DEPURAÇÃO ================
    // ==================================================
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, 0.15f);
    }
#endif
}
