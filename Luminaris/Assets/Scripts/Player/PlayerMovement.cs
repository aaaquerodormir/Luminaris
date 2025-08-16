using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Rigidbody2D rb;

    private bool isFacingRight = false; // Direção inicial do player

    
    [Header("Input Actions")]
    public InputActionReference moveAction; // Referência de movimento
    public InputActionReference jumpAction; // Referência de pulo

    [Header("Movement")]
    public float moveSpeed = 3f;        // Velocidade horizontal máxima
    private float horizontalInput;

    [Header("Jump Settings")]
    public float jumpForce = 8f;              
    public float coyoteTime = 0.1f;            // Tempo para pular após sair do chão
    public float jumpBufferTime = 0.1f;        // Tempo para apertar um botão, antes de tocar o chão e ainda executar o pulo
    public float jumpCutMultiplier = 0.5f;     // Reduz altura do pulo, se soltar o botão cedo
    public float jumpHangGravityMultiplier = 0.5f; // O player flutua no topo do pulo
    public float jumpHangThreshold = 0.1f;     // Velocidade mínima para considerar topo do pulo

    [Header("Gravity")]
    public float gravityScale = 4f;            
    public float fallGravityMultiplier = 2f;   // Aumenta a gravidade durante a queda

    [Header("Ground Check")]
    public Transform groundCheckPos;   // Ponto para saber se o Player está no chão       
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f); // Tamanho da área de verificação
    public LayerMask groundLayer;     // Camada do chão

    private float lastOnGroundTime; // Temporizador do coyote time
    private float lastPressedJumpTime; // Temporizador do jump buffer
    private bool isJumpCut; // Indica se o botão de pulo foi solto cedo, para um pulo mais baixo

    private void Update()
    {
        // Leitura da entrada horizontal (A/D ou Setas)
        horizontalInput = moveAction.action.ReadValue<Vector2>().x;

        // Verifica se o botão de pulo foi pressionado
        if (jumpAction.action.WasPerformedThisFrame())
            lastPressedJumpTime = jumpBufferTime;

        // Verifica se o botão de pulo foi solto cedo
        if (jumpAction.action.WasReleasedThisFrame() && rb.linearVelocity.y > 0)
            isJumpCut = true;

        if (isGrounded())  // Atualiza o coyote time se estiver no chão
            lastOnGroundTime = coyoteTime;
        // Atualiza os timers
        lastOnGroundTime -= Time.deltaTime;
        lastPressedJumpTime -= Time.deltaTime;
        if (lastOnGroundTime > 0 && lastPressedJumpTime > 0) // Vai pular se dentro de coyote e jump buffer
            Jump();
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);  // Movimento horizontal direto

        Flip(); // Inverte a direção do player
    }

    private void FixedUpdate()
    {
        // Gravidade adaptativa
        if (rb.linearVelocity.y < 0) // Caindo
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold) // Topo do pulo
        {
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        }
        else if (isJumpCut) // Botão solto cedo
        {
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false;
        }
        else
        {
            rb.gravityScale = gravityScale; // Gravidade padrão
        }
    }
    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Define a velocidade vertical
        lastOnGroundTime = 0; // Reseta o coyote time
        lastPressedJumpTime = 0; // Reseta o jump buffer
    }
    private bool isGrounded() // Verifica se está no chão
    {
        return Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer);
    }
    private void Flip() // Inverte a direção do player
    {
        if (isFacingRight && horizontalInput < 0 || !isFacingRight && horizontalInput > 0)
        {
            isFacingRight = !isFacingRight;
            Vector3 ls = transform.localScale;
            ls.x *= -1f;
            transform.localScale = ls;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
