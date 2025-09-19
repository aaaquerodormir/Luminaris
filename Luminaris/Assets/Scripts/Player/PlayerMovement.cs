using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMovementUI playerUI;
    public Animator anim;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    public float horizontalInput;

    [Header("Jump Physics")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float jumpHangGravityMultiplier = 0.5f;
    [SerializeField] private float jumpHangThreshold = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;

    public bool isActive = false;

    private float lastOnGroundTime;
    private float lastPressedJumpTime;
    private bool isJumpCut;
    private bool isFacingRight = false;

    private bool waitingToEndTurn = false;
    private bool hasLandedAfterMaxJump = false;
    private bool jumpInitiated = false;
    private bool isInAir = false;

    // Áudio
    private AudioSource walkAudio;

    private void Start()
    {
        // Cria um AudioSource de loop para o som de andar
        walkAudio = AudioManager.Instance.PlayLoop("Andando", gameObject);
        if (walkAudio != null)
            walkAudio.Stop(); // começa parado
    }

    private void Update()
    {
        if (!isActive) return;

        horizontalInput = moveAction.action.ReadValue<Vector2>().x;
        bool grounded = IsGrounded();

        // Controle do áudio de andar
        if (Mathf.Abs(horizontalInput) >= 0.1f && grounded && !waitingToEndTurn)
        {
            if (walkAudio != null && !walkAudio.isPlaying)
                walkAudio.Play();
        }
        else
        {
            if (walkAudio != null && walkAudio.isPlaying)
                walkAudio.Stop();
        }

        // Detecta saída do chão
        if (!grounded && !isInAir)
            isInAir = true;

        // Detecta pouso no chão
        if (grounded && isInAir)
        {
            isInAir = false;

            if (jumpInitiated)
            {
                jumpInitiated = false;

                if (playerUI.JumpsUsed >= playerUI.MaxJumps)
                {
                    waitingToEndTurn = true;

                    if (!hasLandedAfterMaxJump)
                    {
                        hasLandedAfterMaxJump = true;
                        TurnControl.Instance.EndTurnIfReady();
                    }
                }
            }
        }

        // Encerramento de turno
        if (waitingToEndTurn)
        {
            if (grounded && !hasLandedAfterMaxJump)
            {
                hasLandedAfterMaxJump = true;
                rb.linearVelocity = Vector2.zero;
                TurnControl.Instance.EndTurnIfReady();
            }
            else if (!grounded && !hasLandedAfterMaxJump)
            {
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        // Jump Buffer
        if (jumpAction.action.WasPerformedThisFrame() && playerUI.JumpsUsed < playerUI.MaxJumps && !waitingToEndTurn)
        {
            lastPressedJumpTime = jumpBufferTime;
            jumpInitiated = true;

            // Som de pulo
            AudioManager.Instance.PlaySound("Pulando");
        }

        // Jump Cut
        if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
            isJumpCut = true;

        // Coyote Time
        if (grounded)
            lastOnGroundTime = coyoteTime;

        lastOnGroundTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;

        // Executa pulo
        if (lastOnGroundTime > 0 && lastPressedJumpTime > 0 && playerUI.JumpsUsed < playerUI.MaxJumps && !waitingToEndTurn)
            Jump();

        // Movimento horizontal
        if (!waitingToEndTurn || (waitingToEndTurn && !hasLandedAfterMaxJump))
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        Flip();
        HandleAnimations();
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold)
        {
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        }
        else if (isJumpCut)
        {
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastOnGroundTime = 0;
        lastPressedJumpTime = 0;
        playerUI.ConsumeJump();
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
    }

    private void HandleAnimations()
    {
        anim.SetBool("isJumping", rb.linearVelocity.y > .1f);
        anim.SetBool("IsGrounded", IsGrounded());
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        anim.SetBool("isIdle", Mathf.Abs(horizontalInput) < .1f && IsGrounded());
        anim.SetBool("isWalking", Mathf.Abs(horizontalInput) >= .1f && IsGrounded());
    }

    private void Flip()
    {
        if (isFacingRight && horizontalInput < 0 || !isFacingRight && horizontalInput > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;
        }
    }

    public void StartTurn()
    {
        isActive = true;
        waitingToEndTurn = false;
        hasLandedAfterMaxJump = false;
        jumpInitiated = false;
        isInAir = false;
        playerUI.StartTurn();
    }

    public void EndTurn()
    {
        isActive = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        anim.SetBool("isJumping", false);
        anim.SetBool("isWalking", false);
        anim.SetBool("isIdle", true);
        anim.SetBool("IsGrounded", true);
        playerUI.EndTurn();

        if (walkAudio != null && walkAudio.isPlaying)
            walkAudio.Stop();
    }

    public void AddJumpPowerUp(int extraJumps, int duration)
    {
        playerUI.AddJumpPowerUp(extraJumps, duration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
