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
   // [SerializeField] private NetworkAnimator netAnimator;

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


    private bool isActive = false; // controlado via RPCs
    private float moveInput;
    private bool isFacingRight = true;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerUI == null) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerMovement] Spawned — OwnerClientId={OwnerClientId}, IsOwner={IsOwner}");

        if (IsServer)
        {
            Debug.Log($"[PlayerMovement] (Server) Registrando jogador {OwnerClientId} no TurnControl...");
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

        if (playerUI != null && playerUI.RemainingJumps <= 0)
        {
            Debug.Log($"[{gameObject.name}] Sem pulos restantes — encerrando turno automaticamente.");
            EndTurnServerRpc();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isActive) return;
        Move();
    }

    private void Move()
    {
        float velocityX = moveInput * moveSpeed;
        rb.linearVelocity = new Vector2(velocityX, rb.linearVelocity.y);

        if (Mathf.Abs(velocityX) > 0.01f)
            spriteRenderer.flipX = velocityX < 0;

        anim.SetFloat("Speed", Mathf.Abs(velocityX));
    }

    private void TryJump()
    {
        if (!IsGrounded() || playerUI.RemainingJumps <= 0) return;

        playerUI.ConsumeJump();

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        anim.SetTrigger("Jump");

        Debug.Log($"[{gameObject.name}] Pulou! ({playerUI.JumpsUsed}/{playerUI.MaxJumps})");
    }

    private bool IsGrounded()
    {
        Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, 0.15f, groundLayer);
        return hit != null;
    }

    // ====================================================
    // ==== SINCRONIZAÇÃO DE TURNOS E ATIVAÇÃO ============
    // ====================================================

    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        isActive = active;

        if (active)
        {
            playerUI?.StartTurn();
        }
        else
        {
            playerUI?.EndTurn();
        }

        string status = active ? "ATIVO" : "INATIVO";
        Debug.Log($"[{gameObject.name}] Agora está {status}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndTurnServerRpc()
    {
        TurnControl.Instance?.EndTurn();
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

