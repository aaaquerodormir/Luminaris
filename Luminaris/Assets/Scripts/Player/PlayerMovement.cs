using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;

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

    private PlayerInputActions controls;
    private bool isJumpPressed;
    private bool isFacingRight = true;
    private bool isActive = false;
    private float horizontalInput;

    private AudioSource walkAudio;

    private void Awake()
    {
        controls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // Áudio
        walkAudio = AudioManager.Instance.PlayLoop("Andando", gameObject);
        if (walkAudio != null) walkAudio.Stop();

        Debug.Log($"[PlayerMovement] {gameObject.name} iniciado. IsOwner={IsOwner}, IsLocalPlayer={IsLocalPlayer}");
    }

    private void Update()
    {
        // Só o dono local deve enviar inputs
        if (!IsOwner) return;
        if (!isActive) return;

        Vector2 move = controls.Player.Move.ReadValue<Vector2>();
        horizontalInput = move.x;
        isJumpPressed = controls.Player.Jump.WasPressedThisFrame();

        HandleMovement();
        HandleAnimations();
    }

    private void HandleMovement()
    {
        bool grounded = IsGrounded();

        // Movimento horizontal
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        // Pulo
        if (isJumpPressed && grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            AudioManager.Instance.PlaySound("Pulando");
        }

        // Áudio de andar
        if (Mathf.Abs(horizontalInput) > 0.1f && grounded)
        {
            if (!walkAudio.isPlaying) walkAudio.Play();
        }
        else if (walkAudio.isPlaying)
        {
            walkAudio.Stop();
        }

        Flip();
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0f, groundLayer);
    }

    private void Flip()
    {
        if (horizontalInput > 0 && !isFacingRight)
        {
            isFacingRight = true;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        else if (horizontalInput < 0 && isFacingRight)
        {
            isFacingRight = false;
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void HandleAnimations()
    {
        if (anim == null) return;

        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetBool("isWalking", Mathf.Abs(horizontalInput) > 0.1f && IsGrounded());
        anim.SetBool("isIdle", Mathf.Abs(horizontalInput) < 0.1f && IsGrounded());
        anim.SetBool("isJumping", rb.linearVelocity.y > 0.1f);
    }

    public void StartTurn()
    {
        isActive = true;
        Debug.Log($"[PlayerMovement] {gameObject.name} — turno iniciado");
    }

    public void EndTurn()
    {
        isActive = false;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        Debug.Log($"[PlayerMovement] {gameObject.name} — turno finalizado");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
