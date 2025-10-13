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


    private bool isTurnActive;
    private float moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerUI == null) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            TurnControl.Instance?.RegisterPlayer(this);
        }

        Debug.Log($"[PlayerMovement] Spawned — OwnerClientId={OwnerClientId}, IsOwner={IsOwner}");
    }

    private void Update()
    {
        if (!IsOwner || !isTurnActive) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && playerUI.CanJump())
        {
            JumpServerRpc();
        }

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isTurnActive) return;

        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    [ServerRpc]
    private void JumpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (playerUI == null) return;

        if (playerUI.CanJump())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            playerUI.ConsumeJump();
            JumpClientRpc();
        }
        else
        {
            Debug.Log($"[PlayerMovement] Player {OwnerClientId} tentou pular, mas não tem pulos restantes.");
            TurnControl.Instance?.EndTurn();
        }
    }

    [ClientRpc]
    private void JumpClientRpc()
    {
        if (anim != null)
            anim.SetTrigger("Jump");
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        isTurnActive = active;

        if (active)
            playerUI?.StartTurn();
        else
            playerUI?.EndTurn();

        Debug.Log($"[PlayerMovement] Turno do jogador {OwnerClientId}: {(active ? "Ativo" : "Inativo")}");
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetBool("isRunning", Mathf.Abs(moveInput) > 0.1f);
        anim.SetBool("isGrounded", isGrounded);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}

