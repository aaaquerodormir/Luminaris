using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(NetworkRigidbody2D))]

public class PlayerMovement : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    //[SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private NetworkAnimator netAnimator;


    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 8f;


    [Header("Ground Check")]
    //[SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    [Header("Controle de Pulo")]
    [SerializeField] private int maxJumps = 3;
    public readonly NetworkVariable<int> CompletedJumpsNet = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private bool pendingJump = false;
    public event System.Action<ulong, int> OnTurnStarted;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = false;
    private Vector2 moveInput;

    // sincroniza flip entre host/client
    private readonly NetworkVariable<bool> netFacingRight =
       new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ==============================
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        //if (!playerUI) playerUI = GetComponent<PlayerMovementUI>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        CompletedJumpsNet.OnValueChanged += HandleJumpsChanged;

        if (IsOwner) EnableInputs();
        else DisableInputs();

        Debug.Log($"[PlayerMovement:{name}] NetworkSpawn — Owner={IsOwner} ({OwnerClientId})");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner) DisableInputs();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();
        CompletedJumpsNet.OnValueChanged -= HandleJumpsChanged; // Desassina
    }

    // CALLBACK DE REDE: Dispara em TODOS os clientes quando CompletedJumpsNet muda
    private void HandleJumpsChanged(int oldVal, int newVal)
    {
        int remaining = maxJumps - newVal;
        // Chama o método para todos os HUDs atualizarem sua exibição
        UpdateJumpsClientRpc(remaining);
    }

    // CLIENT RPC: Garante que a UI seja atualizada em todos
    [ClientRpc]
    private void UpdateJumpsClientRpc(int remainingJumps)
    {
        // 🔑 MUDANÇA AQUI: Chame o método público estático para disparar o evento
        JumpHUD.NotifyJumpsChanged(OwnerClientId, remainingJumps); // <-- CORREÇÃO

        Debug.Log($"[SYNC-RPC:{OwnerClientId}] Novo total de pulos: {remainingJumps}");
    }

    // ==============================
    private void EnableInputs()
    {
        moveAction?.action.Enable();
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJump;
        }
    }

    private void DisableInputs()
    {
        moveAction?.action.Disable();
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJump;
        }
    }

    // ==============================
    private void Update()
    {
        CheckGround();
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        float yVel = rb.linearVelocity.y;
        UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);

        if (!IsOwner || !isMyTurn) return;

        moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float moveX = moveInput.x;
        rb.linearVelocity = new Vector2(moveX * moveSpeed, rb.linearVelocity.y);

        if (moveX > 0 && !facingRight)
            SetFacingServerRpc(true);
        else if (moveX < 0 && facingRight)
            SetFacingServerRpc(false);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn || !isGrounded) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        pendingJump = true;

        netAnimator.SetTrigger("Jump");
        Debug.Log($"[PlayerMovement:{name}] ⬆️ Pulou (aguardando aterrissagem)");
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded && !wasGrounded)
        {
            if (pendingJump)
            {
                pendingJump = false;

                // NOVO: Apenas o Server pode modificar a NetworkVariable
                if (IsServer)
                {
                    // Se for o Host, ele modifica diretamente e o callback dispara
                    CompletedJumpsNet.Value++;
                }
                else // Se for o Client, ele precisa solicitar ao Server para modificar
                {
                    // RPC para solicitar a contagem de pulos
                    SubmitJumpServerRpc();
                }

                Debug.Log($"[PlayerMovement:{name}] 🟢 Aterrissou.");
            }

            // A condição de fim de turno deve usar o valor SINCRONIZADO
            if (CompletedJumpsNet.Value >= maxJumps)
            {
                Debug.Log($"[PlayerMovement:{name}] 🚩 Máximo de pulos atingido — fim de turno!");
                RequestEndTurn();
            }
        }
    }

    // RPC do Client para o Server: solicita incremento de pulo
    [ServerRpc(RequireOwnership = false)]
    private void SubmitJumpServerRpc(ServerRpcParams rpcParams = default)
    {
        // Garante que apenas o Host ou o dono do objeto pode fazer a alteração.
        if (rpcParams.Receive.SenderClientId != OwnerClientId && !IsServer) return;

        CompletedJumpsNet.Value++; // O Server faz a mudança
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateAnimatorServerRpc(bool moving, bool grounded, float yVel, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId) return;
        Animator a = netAnimator.Animator;
        if (a == null) return;
        a.SetBool("IsMoving", moving);
        a.SetBool("IsGrounded", grounded);
        a.SetFloat("yVelocity", yVel);
    }

    private void UpdateFacingDirection()
    {
        bool newFacing = netFacingRight.Value;
        if (newFacing == facingRight) return;

        facingRight = newFacing;
        if (spriteRenderer != null)
            spriteRenderer.flipX = !facingRight;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetFacingServerRpc(bool right)
    {
        if (netFacingRight.Value != right)
            netFacingRight.Value = right;
    }

    private void RequestEndTurn()
    {
        if (IsServer)
            TurnControl.Instance?.EndTurn();
        else
            SubmitEndTurnServerRpc();
    }

    [ServerRpc]
    private void SubmitEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        TurnControl.Instance?.EndTurn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTurnActiveServerRpc(bool active)
    {
        SetTurnActiveClientRpc(active);
    }

    [ClientRpc]
    private void SetTurnActiveClientRpc(bool active)
    {
        SetTurnActive(active);
    }

    private void SetTurnActive(bool active)
    {
        isMyTurn = active;
        if (!IsOwner) return;

        if (active)
        {
            // NOVO: Apenas o Server reseta a contagem
            if (IsServer)
            {
                CompletedJumpsNet.Value = 0;
            }
            // NOVO: Dispara evento para o HUD (útil para HUDs que precisam saber o máximo)
            OnTurnStarted?.Invoke(OwnerClientId, maxJumps);
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}


