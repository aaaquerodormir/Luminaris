using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    private bool isFacingRight = false;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    public float horizontalInput;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float coyoteTime = 0.1f; // tempo extra após sair da plataforma
    [SerializeField] private float jumpBufferTime = 0.1f; // tempo para "guardar" o pulo antes de tocar o chão
    [SerializeField] private float jumpCutMultiplier = 0.5f; // corta altura do pulo ao soltar botão cedo
    [SerializeField] private float jumpHangGravityMultiplier = 0.5f; // gravidade reduzida durante "hang time"
    [SerializeField] private float jumpHangThreshold = 0.1f; // velocidade baixa → ativa hang time

    [Header("Gravity")]
    [SerializeField] private float gravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2f; // gravidade aumentada na queda

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheckPos;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    [SerializeField] private LayerMask groundLayer;

    private float lastOnGroundTime;     // tempo desde que saiu do chão (para coyote time)
    private float lastPressedJumpTime;  // tempo desde que apertou pulo (para jump buffer)
    private bool isJumpCut;             // flag para cortar altura do pulo

    public bool isActive = false;
    private int jumpCount = 0;

    // Controle de turnos
    private bool waitingToEndTurn = false;
    private bool hasLandedAfterThirdJump = false;
    private bool jumpInitiated = false;
    private bool isInAir = false;

    private void Update()
    {
        if (!isActive) return;

        // Lê input horizontal
        horizontalInput = moveAction.action.ReadValue<Vector2>().x;
        bool grounded = IsGrounded();

        // Detecta saída do chão
        if (!grounded && !isInAir)
            isInAir = true;

        // Detecta pouso no chão
        if (grounded && isInAir)
        {
            isInAir = false;

            if (jumpInitiated)
            {
                jumpCount++;
                jumpInitiated = false;

                // Se fez 3 pulos → marca para encerrar turno
                if (jumpCount >= 3)
                {
                    waitingToEndTurn = true;
                    hasLandedAfterThirdJump = false;
                }
            }
        }

        // Encerramento de turno
        if (waitingToEndTurn)
        {
            if (grounded && !hasLandedAfterThirdJump)
            {
                hasLandedAfterThirdJump = true;
                TurnControl.Instance.EndTurnIfReady();
            }
            else
            {
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
            }
            return;
        }

        // Jump Buffer → registra intenção de pulo
        if (jumpAction.action.WasPerformedThisFrame() && jumpCount < 3 && !waitingToEndTurn)
        {
            lastPressedJumpTime = jumpBufferTime;
            jumpInitiated = true;
        }

        // Jump Cut → corta altura se soltar botão antes do ápice
        if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
            isJumpCut = true;

        // Coyote Time → ainda permite pulo logo após sair da borda
        if (grounded)
            lastOnGroundTime = coyoteTime;

        lastOnGroundTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;

        // Executa pulo se dentro das janelas de buffer e coyote
        if (lastOnGroundTime > 0 && lastPressedJumpTime > 0 && jumpCount < 3 && !waitingToEndTurn)
            Jump();

        // Movimento horizontal normal
        if (!waitingToEndTurn || (waitingToEndTurn && !hasLandedAfterThirdJump))
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        Flip();
    }

    private void FixedUpdate()
    {
        // Gravidade customizada
        if (rb.linearVelocity.y < 0)
        {
            // Aumenta gravidade na queda
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold)
        {
            // Gravidade menor no "hang time"
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        }
        else if (isJumpCut)
        {
            // Corta pulo se soltar botão cedo
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false;
        }
        else
        {
            // Gravidade normal
            rb.gravityScale = gravityScale;
        }
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // Reseta variáveis de controle
        lastOnGroundTime = 0;
        lastPressedJumpTime = 0;
        isJumpCut = false;
    }

    public bool IsGrounded()
    {
        // Checa colisão no chão
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
    }

    private void Flip()
    {
        // Vira o sprite baseado na direção do movimento
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
        // Reseta variáveis ao iniciar turno
        isActive = true;
        jumpCount = 0;
        waitingToEndTurn = false;
        hasLandedAfterThirdJump = false;
        jumpInitiated = false;
        isInAir = false;
    }

    public void EndTurn()
    {
        // Desativa controle do jogador
        isActive = false;
        horizontalInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo para debug da área de detecção do chão
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
