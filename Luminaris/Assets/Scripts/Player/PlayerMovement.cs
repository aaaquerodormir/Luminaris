using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] private int baseMaxJumps = 3;
    // Adicione a NetworkVariable
    public readonly NetworkVariable<int> CompletedJumpsNet = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    public readonly NetworkVariable<int> MaxJumpsNet = new(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // ⬇️ NOVO: Rastreia a duração do power-up de pulo ⬇️
    public readonly NetworkVariable<int> JumpBuffTurnsLeft = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // ⬇️ NOVO: Apenas o servidor usa isso para lembrar o quanto reverter ⬇️
    private int server_extraJumpsApplied = 0;
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

        // ⬇️ MODIFICAÇÃO AQUI ⬇️
        // Removemos o 'HandleJumpsChanged' antigo.
        // CompletedJumpsNet.OnValueChanged += HandleJumpsChanged; // <--- REMOVA/COMENTE ESTA LINHA

        // Precisamos ouvir AMBAS as variáveis que afetam os pulos restantes.
        // Usamos a mesma função de callback para as duas.
        CompletedJumpsNet.OnValueChanged += OnJumpVariablesChanged;
        MaxJumpsNet.OnValueChanged += OnJumpVariablesChanged;
        // ⬆️ FIM DA MODIFICAÇÃO ⬆️

        if (IsOwner) EnableInputs();
        else DisableInputs();

        Debug.Log($"[PlayerMovement:{name}] NetworkSpawn — Owner={IsOwner} ({OwnerClientId})");

        // ⬇️ NOVO: Garante que a UI tenha o valor correto no Spawn ⬇️
        // (Isso é bom para jogadores que se conectam tarde)
        // Disparamos manualmente no início para sincronizar a UI com o estado inicial.
        OnJumpVariablesChanged(0, 0);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner) DisableInputs();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();

        // ⬇️ MODIFICAÇÃO AQUI ⬇️
        // CompletedJumpsNet.OnValueChanged -= HandleJumpsChanged; // <--- REMOVA/COMENTE ESTA LINHA

        // Garante que estamos desassinando os novos callbacks
        CompletedJumpsNet.OnValueChanged -= OnJumpVariablesChanged;
        MaxJumpsNet.OnValueChanged -= OnJumpVariablesChanged;
        // ⬆️ FIM DA MODIFICAÇÃO ⬆️
    }

    // ⬇️ ESTE É O NOVO MÉTODO CENTRAL ⬇️
    /// <summary>
    /// Este callback é disparado em TODOS OS CLIENTES quando
    /// MaxJumpsNet ou CompletedJumpsNet mudam de valor na rede.
    /// </summary>
    private void OnJumpVariablesChanged(int oldVal, int newVal)
    {
        // 1. Calcula o valor restante
        int remaining = MaxJumpsNet.Value - CompletedJumpsNet.Value;

        // 2. Dispara o evento estático do JumpHUD.
        // O JumpHUD (sendo uma classe de UI local) está escutando este evento
        // e filtrará pelo OwnerClientId correto.
        JumpHUD.NotifyJumpsChanged(OwnerClientId, remaining);
    }

    // CALLBACK DE REDE: Dispara em TODOS os clientes quando CompletedJumpsNet muda
    //private void HandleJumpsChanged(int oldVal, int newVal)
    //{
    //    int remaining = MaxJumpsNet.Value - newVal;
    //    // Chama o método para todos os HUDs atualizarem sua exibição
    //    UpdateJumpsClientRpc(remaining);
    //}

    // CLIENT RPC: Garante que a UI seja atualizada em todos
    //[ClientRpc]
    //private void UpdateJumpsClientRpc(int remainingJumps)
    //{
    //    // 🔑 MUDANÇA AQUI: Chame o método público estático para disparar o evento
    //    JumpHUD.NotifyJumpsChanged(OwnerClientId, remainingJumps); // <-- CORREÇÃO

    //    Debug.Log($"[SYNC-RPC:{OwnerClientId}] Novo total de pulos: {remainingJumps}");
    //}

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
                    CompletedJumpsNet.Value++;
                }
                else // Se for o Client, ele precisa solicitar ao Server para modificar
                {
                    SubmitJumpServerRpc();
                }

                Debug.Log($"[PlayerMovement:{name}] 🟢 Aterrissou.");
            }

            // CORREÇÃO: Só encerra o turno se for o turno ativo E atingiu o limite de pulos
            if (isMyTurn && CompletedJumpsNet.Value >= MaxJumpsNet.Value)
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
            OnTurnStarted?.Invoke(OwnerClientId, MaxJumpsNet.Value);
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
    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlatformTouchServerRpc(NetworkObjectReference platformNetObj, ServerRpcParams rpcParams = default)
    {
        // ... (código que notifica a plataforma, este está CORRETO)
        if (platformNetObj.TryGet(out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PlataformaInstavel platform))
            {
                platform.ActivateFallFromServer();
            }
        }
    }

    // 3. Adicione a função pública que o PowerUp chamará (SÓ NO SERVIDOR)
    public void ApplyJumpPowerUp(int extraJumps, int durationTurns)
    {
        if (!IsServer) return;

        // 2. SOMA o novo bônus ao valor de rede
        MaxJumpsNet.Value += extraJumps;

        // 3. SOMA o bônus ao nosso rastreador de servidor (para reverter DEPOIS)
        server_extraJumpsApplied += extraJumps;

        // 4. Define a duração para o MÁXIMO entre a atual e a nova
        JumpBuffTurnsLeft.Value = Mathf.Max(JumpBuffTurnsLeft.Value, durationTurns);

        Debug.Log($"[PlayerMovement-SERVER] {name} (ID: {OwnerClientId}) ACUMULOU {extraJumps} pulos. Total extra: {server_extraJumpsApplied}. Duração: {JumpBuffTurnsLeft.Value} turnos.");
    }

    public void DecrementBuffTurns()
    {
        if (!IsServer || JumpBuffTurnsLeft.Value == 0) return;

        JumpBuffTurnsLeft.Value--;

        if (JumpBuffTurnsLeft.Value <= 0)
        {
            RevertJumpPowerUp(); // Esta função já está correta
        }
    }

    private void RevertJumpPowerUp()
    {
        if (!IsServer) return;

        // Esta função já funciona para o acúmulo, pois ela remove
        // o valor total que rastreamos em 'server_extraJumpsApplied'.
        MaxJumpsNet.Value -= server_extraJumpsApplied;
        server_extraJumpsApplied = 0;
        JumpBuffTurnsLeft.Value = 0; // Garante que está zerado

        Debug.Log($"[PlayerMovement-SERVER] {name} (ID: {OwnerClientId}) PowerUp de pulo expirou.");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ... (lógica existente)

        // VERIFICAÇÃO DE COLISÃO COM PLATAFORMA (Não requer Tag, usa o Componente)
        if (IsOwner)
        {
            if (collision.gameObject.TryGetComponent(out PlataformaInstavel platform))
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    // 🔑 CORREÇÃO AQUI: A normal.y deve ser POSITIVA (apontando para cima) para ser o topo.
                    if (contact.normal.y > 0.5f) // <--- CONDIÇÃO CORRIGIDA!
                    {
                        if (platform.NetworkObject.IsSpawned)
                        {
                            NotifyPlatformTouchServerRpc(platform.NetworkObject);
                        }
                        break;
                    }
                }
            }
        }
    }
}


