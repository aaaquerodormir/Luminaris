using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
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
    [SerializeField] private int baseMaxJumps = 3; // adicionei baseMaxJumps
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
    private bool hasLandedAfterMaxJump = false;
    private bool jumpInitiated = false;
    private bool isInAir = false;

    // Lista de power ups ativos
    private List<(int extraJumps, int turnsLeft)> activeJumpPowerUps = new();

    public int MaxJumps
    {
        get
        {
            int total = baseMaxJumps;
            foreach (var power in activeJumpPowerUps)
                total += power.extraJumps;
            return total;
        }
    }

    private void Update()
    {
        if (!isActive) return; // só o player ativo pode processar input

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
                jumpInitiated = false;

                // 🔥 Se já usou todos os pulos, marca para encerrar
                if (jumpCount >= MaxJumps)
                {
                    waitingToEndTurn = true;

                    // Se já está no chão ao pousar → troca de turno imediatamente
                    if (!hasLandedAfterMaxJump)
                    {
                        hasLandedAfterMaxJump = true;
                        TurnControl.Instance.EndTurnIfReady();
                    }
                }
            }
        }

        // Encerramento de turno (segurança extra caso já estivesse no chão)
        if (waitingToEndTurn)
        {
            if (grounded && !hasLandedAfterMaxJump)
            {
                hasLandedAfterMaxJump = true;
                rb.linearVelocity = Vector2.zero; // 🔥 trava o movimento imediatamente
                TurnControl.Instance.EndTurnIfReady();
            }
            else if (!grounded && !hasLandedAfterMaxJump)
            {
                // ainda deixa mexer no ar mesmo no último pulo
                rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                // já pousou, não mexe mais
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        // Jump Buffer
        if (jumpAction.action.WasPerformedThisFrame() && jumpCount < MaxJumps && !waitingToEndTurn)
        {
            lastPressedJumpTime = jumpBufferTime;
            jumpInitiated = true;
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
        if (lastOnGroundTime > 0 && lastPressedJumpTime > 0 && jumpCount < MaxJumps && !waitingToEndTurn)
            Jump();

        // Movimento horizontal normal
        if (!waitingToEndTurn || (waitingToEndTurn && !hasLandedAfterMaxJump))
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
        lastOnGroundTime = 0;
        lastPressedJumpTime = 0;

        jumpCount++; // ✅ agora só conta pulo aqui
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
        hasLandedAfterMaxJump = false;
        jumpInitiated = false;
        isInAir = false;

        // Consome turnos dos power ups
        for (int i = activeJumpPowerUps.Count - 1; i >= 0; i--)
        {
            var p = activeJumpPowerUps[i];
            p.turnsLeft--;
            if (p.turnsLeft <= 0)
                activeJumpPowerUps.RemoveAt(i);
            else
                activeJumpPowerUps[i] = p;
        }

        Debug.Log($"{gameObject.name} começou o turno com {MaxJumps} pulos.");
    }

    public void EndTurn()
    {
        // Desativa controle do jogador
        isActive = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public void AddJumpPowerUp(int extraJumps, int duration)
    {
        activeJumpPowerUps.Add((extraJumps, duration));
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo para debug da área de detecção do chão
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(groundCheckPos.position, groundCheckSize);
    }
}
