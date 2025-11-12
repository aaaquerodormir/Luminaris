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
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private NetworkAnimator netAnimator;

    [Header("Jump Physics (Integrado)")]
    [SerializeField] private float jumpForce = 8f;
    //[SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float jumpHangGravityMultiplier = 0.5f;
    [SerializeField] private float jumpHangThreshold = 0.1f;

    [Header("Gravity (Integrado)")]
    [SerializeField] private float gravityScale = 4f;
    [SerializeField] private float fallGravityMultiplier = 2f;

    [SerializeField] private float moveSpeed = 3f;


    [Header("Ground Check")]
    //[SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;

    //private float lastOnGroundTime;
    private float lastPressedJumpTime;
    private bool isJumpCut;

    [Header("Controle de Pulo")]
    //[SerializeField] private int baseMaxJumps = 3;
    // Adicione a NetworkVariable
    public readonly NetworkVariable<int> CompletedJumpsNet = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    public readonly NetworkVariable<int> MaxJumpsNet = new(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    //Rastreia a duração do power-up de pulo
    public readonly NetworkVariable<int> JumpBuffTurnsLeft = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    private int server_extraJumpsApplied = 0;
    private bool pendingJump = false;
    public event System.Action<ulong, int> OnTurnStarted;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private bool isGrounded;
    private bool wasGrounded;
    private bool isMyTurn = false;
    private bool facingRight = true;
    private Vector2 moveInput;

    private AudioSource walkAudio;

    // sincroniza flip entre host/client
    private readonly NetworkVariable<bool> netFacingRight =
       new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!netAnimator) netAnimator = GetComponent<NetworkAnimator>();
        rb.gravityScale = gravityScale;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        netFacingRight.OnValueChanged += (_, __) => UpdateFacingDirection();

        CompletedJumpsNet.OnValueChanged += OnJumpVariablesChanged;
        MaxJumpsNet.OnValueChanged += OnJumpVariablesChanged;

        if (IsOwner)
        {
            EnableInputs();

            if (AudioManager.Instance != null)
            {
                walkAudio = AudioManager.Instance.PlayLoop("Andando", gameObject);
                if (walkAudio != null)
                    walkAudio.Stop(); // Começa parado
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] AudioManager.Instance não encontrado!");
            }
        }
        else
        {
            DisableInputs();
        }

        Debug.Log($"[PlayerMovement:{name}] NetworkSpawn — Owner={IsOwner} ({OwnerClientId})");
        OnJumpVariablesChanged(0, 0);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (IsOwner) DisableInputs();
        netFacingRight.OnValueChanged -= (_, __) => UpdateFacingDirection();
        CompletedJumpsNet.OnValueChanged -= OnJumpVariablesChanged;
        MaxJumpsNet.OnValueChanged -= OnJumpVariablesChanged;
    }

    private void OnJumpVariablesChanged(int oldVal, int newVal)
    {
        int remaining = MaxJumpsNet.Value - CompletedJumpsNet.Value;
        JumpHUD.NotifyJumpsChanged(OwnerClientId, remaining);
    }

    private void EnableInputs()
    {
        moveAction?.action.Enable();
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            // 'Performed' ativa o Jump Buffer
            jumpAction.action.performed += OnJumpPressed;
            // 'Canceled' ativa o Jump Cut
            jumpAction.action.canceled += OnJumpReleased;
        }
    }

    private void DisableInputs()
    {
        moveAction?.action.Disable();
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJumpPressed;
            jumpAction.action.canceled -= OnJumpReleased;
        }
    }

    // Chamado quando o botão de pulo é PRESSIONADO
    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;

        // Ativa o Jump Buffer
        lastPressedJumpTime = jumpBufferTime;
    }

    // Chamado quando o botão de pulo é SOLTO
    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        if (!IsOwner || !isMyTurn) return;

        // Ativa o Jump Cut
        if (rb.linearVelocity.y > 0)
            isJumpCut = true;
    }
    private void Update()
    {
        CheckGround();

        // Atualiza animações para todos
        bool isMoving = Mathf.Abs(moveInput.x) > 0.1f;
        float yVel = rb.linearVelocity.y;
        UpdateAnimatorServerRpc(isMoving, isGrounded, yVel);

        if (!IsOwner || !isMyTurn)
        {

            lastPressedJumpTime = 0;
            return;
        }

        moveInput = moveAction.action.ReadValue<Vector2>();

        if (walkAudio != null)
        {
            bool isMovingOnGround = isMoving && isGrounded;
            if (isMovingOnGround && !walkAudio.isPlaying)
            {
                walkAudio.Play();
            }
            else if (!isMovingOnGround && walkAudio.isPlaying)
            {
                walkAudio.Stop();
            }
        }
        lastPressedJumpTime -= Time.deltaTime;
        bool hasJumpsLeft = CompletedJumpsNet.Value < MaxJumpsNet.Value;
        if (lastPressedJumpTime > 0 && isGrounded && hasJumpsLeft && !pendingJump)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isMyTurn)
        {
            // Zera a gravidade customizada se não for nosso turno
            rb.gravityScale = gravityScale;
            return;
        }

        // 1. Aplicar movimento horizontal (Lógica Atual)
        HandleMovement();

        // 2. Aplicar física de gravidade customizada (Lógica Antiga)
        if (rb.linearVelocity.y < 0)
        {
            // Queda mais rápida
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else if (Mathf.Abs(rb.linearVelocity.y) < jumpHangThreshold)
        {
            // No ápice do pulo, gravidade menor (Hang Time)
            rb.gravityScale = gravityScale * jumpHangGravityMultiplier;
        }
        else if (isJumpCut)
        {
            // Corta o pulo (Jump Cut)
            rb.gravityScale = gravityScale; // Gravidade normal
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            isJumpCut = false; // Só aplica uma vez
        }
        else
        {
            // Subida normal
            rb.gravityScale = gravityScale;
        }
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

    private void Jump()
    {
        if (!IsOwner) return; // Segurança

        // Aplica a força
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        lastPressedJumpTime = 0;

        // Ativa flags
        isJumpCut = false; // Garante que não corte imediatamente
        pendingJump = true; // Flag da lógica de rede (espera aterrissar para contar)

        // Feedback
        netAnimator.SetTrigger("Jump");

        // ==== ÁUDIO (INTEGRADO) ====
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Pulando");
        }

        Debug.Log($"[PlayerMovement:{name}] ⬆️ Pulou (aguardando aterrissagem)");
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (isGrounded)
        {
        }

        if (isGrounded && !wasGrounded)
        {
            // Se estávamos esperando um pulo ser contado...
            if (pendingJump)
            {
                pendingJump = false;

                // Apenas o Server pode modificar a NetworkVariable
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

            // Só encerra o turno se for o turno ativo E atingiu o limite de pulos
            if (isMyTurn && CompletedJumpsNet.Value >= MaxJumpsNet.Value)
            {
                Debug.Log($"[PlayerMovement:{name}] 🚩 Máximo de pulos atingido — fim de turno!");
                RequestEndTurn();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitJumpServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId && !IsServer) return;
        CompletedJumpsNet.Value++;
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
            spriteRenderer.flipX = facingRight;
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
            if (IsServer)
            {
                CompletedJumpsNet.Value = 0;
            }
            OnTurnStarted?.Invoke(OwnerClientId, MaxJumpsNet.Value);
        }
        else
        {
            moveInput = Vector2.zero;
            rb.linearVelocity = Vector2.zero;

            if (walkAudio != null && walkAudio.isPlaying)
                walkAudio.Stop();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

    // (Restante do código: PowerUps, Colisão, etc. - Sem modificações)
    [ServerRpc(RequireOwnership = false)]
    public void NotifyPlatformTouchServerRpc(NetworkObjectReference platformNetObj, ServerRpcParams rpcParams = default)
    {
        if (platformNetObj.TryGet(out NetworkObject netObj))
        {
            if (netObj.TryGetComponent(out PlataformaInstavel platform))
            {
                platform.ActivateFallFromServer();
            }
        }
    }
    public void ApplyJumpPowerUp(int extraJumps, int durationTurns)
    {
        if (!IsServer) return;
        MaxJumpsNet.Value += extraJumps;
        server_extraJumpsApplied += extraJumps;
        JumpBuffTurnsLeft.Value = Mathf.Max(JumpBuffTurnsLeft.Value, durationTurns);
        Debug.Log($"[PlayerMovement-SERVER] {name} (ID: {OwnerClientId}) ACUMULOU {extraJumps} pulos. Total extra: {server_extraJumpsApplied}. Duração: {JumpBuffTurnsLeft.Value} turnos.");
    }

    public void DecrementBuffTurns()
    {
        if (!IsServer || JumpBuffTurnsLeft.Value == 0) return;
        JumpBuffTurnsLeft.Value--;
        if (JumpBuffTurnsLeft.Value <= 0)
        {
            RevertJumpPowerUp();
        }
    }

    private void RevertJumpPowerUp()
    {
        if (!IsServer) return;
        MaxJumpsNet.Value -= server_extraJumpsApplied;
        server_extraJumpsApplied = 0;
        JumpBuffTurnsLeft.Value = 0;
        Debug.Log($"[PlayerMovement-SERVER] {name} (ID: {OwnerClientId}) PowerUp de pulo expirou.");
    }
    [ServerRpc]
    void ContaPortaServerRpc(int valor)
    {
        Debug.Log("Contandoooooooooo");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsOwner) return;
        PlataformaInstavel platform;
        if (!collision.gameObject.TryGetComponent(out platform)) return;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f && platform.NetworkObject.IsSpawned)
            {
                NotifyPlatformTouchServerRpc(platform.NetworkObject);
                break;
            }
        }
    }
}