using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;

    private bool isFacingRight = false;

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;

    [Header("Movement")]
    public float moveSpeed = 3f;
    private float horizontalInput;

    [Header("Jump Settings")]
    public float jumpForce = 8f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public float jumpHangGravityMultiplier = 0.5f;
    public float jumpHangThreshold = 0.1f;

    [Header("Gravity")]
    public float gravityScale = 4f;
    public float fallGravityMultiplier = 2f;

    [Header("Ground Check")]
    public Transform groundCheckPos;
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f);
    public LayerMask groundLayer;

    private float lastOnGroundTime;
    private float lastPressedJumpTime;
    private bool isJumpCut;

    private int jumpCount = 0;

    public bool isActive = false;

    private bool waitingToEndTurn = false;
    private bool hasLandedAfterThirdJump = false;

    // NOVO: flag para indicar que o player iniciou um pulo (clicou no botão)
    private bool jumpInitiated = false;

    // Controle de estado no ar para detectar pouso
    private bool isInAir = false;

    private void Update()
    {
        if (!isActive)
            return;

        horizontalInput = moveAction.action.ReadValue<Vector2>().x;

        bool grounded = IsGrounded();

        // Detecta saída do chão
        if (!grounded && !isInAir)
        {
            isInAir = true;
        }

        // Detecta pouso no chão
        if (grounded && isInAir)
        {
            isInAir = false;

            // Incrementa jumpCount somente se um pulo foi iniciado antes
            if (jumpInitiated)
            {
                jumpCount++;
                jumpInitiated = false; // Reseta a flag

                Debug.Log($"[DEBUG] Player pousou após pulo. jumpCount = {jumpCount}");

                if (jumpCount >= 3)
                {
                    waitingToEndTurn = true;
                    hasLandedAfterThirdJump = false;
                }
            }
        }

        if (waitingToEndTurn)
        {
            if (grounded && !hasLandedAfterThirdJump)
            {
                hasLandedAfterThirdJump = true;
                TurnControl.Instance.EndTurnIfReady();
                Debug.Log("[DEBUG] Player pousou após 3º pulo. Finalizando turno.");
            }
            else
            {
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
            }
            return;
        }

        // Quando o botão de pulo for apertado, marca que iniciou um pulo, mas não incrementa jumpCount aqui
        if (jumpAction.action.WasPerformedThisFrame() && jumpCount < 3 && !waitingToEndTurn)
        {
            lastPressedJumpTime = jumpBufferTime;
            jumpInitiated = true; // sinaliza que o jogador quer pular
        }

        if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
            isJumpCut = true;

        if (grounded)
            lastOnGroundTime = coyoteTime;

        lastOnGroundTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;

        if (lastOnGroundTime > 0 && lastPressedJumpTime > 0 && jumpCount < 3 && !waitingToEndTurn)
            Jump();

        if (!waitingToEndTurn || (waitingToEndTurn && !hasLandedAfterThirdJump))
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        Flip();
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.y < 0)
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold)
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        else if (isJumpCut)
        {
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false;
        }
        else
            rb.gravityScale = gravityScale;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        lastOnGroundTime = 0;
        lastPressedJumpTime = 0;
        isJumpCut = false;

        Debug.Log("[DEBUG] Executando Jump");
    }

    private bool IsGrounded()
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
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
        jumpCount = 0;
        waitingToEndTurn = false;
        hasLandedAfterThirdJump = false;
        jumpInitiated = false;
        isInAir = false;
    }

    public void EndTurn()
    {
        isActive = false;
        horizontalInput = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
