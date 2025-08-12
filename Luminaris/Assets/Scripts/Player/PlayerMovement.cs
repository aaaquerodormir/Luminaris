using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    [Header("Movement")]
    public float moveSpeed = 5f; // Velocidade de movimento do personagem
    float horizontalMovement;

    [Header("Jumping")]
    public float jumpForce = 5f; // For�a do pulo

    [Header("Ground Check")]
    public Transform groundCheckPos; // Posi��o do ponto de verifica��o do ch�o
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.05f); // Tamanho da �rea de verifica��o do ch�o
    public LayerMask groundLayer; // Camada do ch�o
    void Update()
    {
        rb.linearVelocity = new Vector2(horizontalMovement * moveSpeed, rb.linearVelocity.y); // Movimento horizontal do personagem, por meio da velocidade linear do Rigidbody2D
    }
    public void Move(InputAction.CallbackContext context) => horizontalMovement = context.ReadValue<Vector2>().x; // L� o valor do movimento horizontal do InputAction e atualiza a vari�vel horizontalMovement

    public void Jump(InputAction.CallbackContext context)
    {
       if (isGrounded() && context.performed)
       {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce); // Pressionar totalmente o bot�o de pular
        }
       else if (context.canceled && rb.linearVelocity.y > 0)
       {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f); // Somente um toque no bot�o de pular
        }
    }
    private bool isGrounded()
    { 
        if (Physics2D.OverlapBox(groundCheckPos.position, groundCheckSize, 0, groundLayer))
        {
            return true;
        }
      return false;
    }
    private void OnDrawGizmosSelected() // Gizmos para checar o visual, se o personagem esta tocando o ch�o corretamente, apagar depois de testar
    {
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
