using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(Animator))]
public class EnemyController : NetworkBehaviour
{
    public enum EnemyState
    {
        Spawning,
        Idle,
        Pursuing,
        Waiting,
        Attacking
    }

    [Header("Configurações de Perseguição")]
    [SerializeField]
    private float moveSpeed = 2.5f;
    [Header("Debuff Settings")]
    [SerializeField]
    private int debuffDurationTurns = 2;

    [Header("Referências")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    [Tooltip("Arraste TODOS os GameObjects de partículas que devem ser desativados no ataque.")]
    private GameObject[] particleEffects;

    private Rigidbody2D rb;

    private NetworkVariable<EnemyState> currentState = new NetworkVariable<EnemyState>(EnemyState.Spawning);
    private NetworkVariable<ulong> linkedPlayerId = new NetworkVariable<ulong>(99);

    private Transform targetPlayerTransform;
    private PlayerState targetPlayerState;
    private bool canPursue = false;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (IsServer)
        {
            // --- MUDANÇA 1: Animação de Spawn ---
            // O Servidor dispara a animação de "Spawn" (mude "Spawn" para o nome
            // do seu trigger no Animator). O NetworkAnimator sincroniza.
            animator.SetTrigger("Spawn");
            // ------------------------------------

            TurnControl.OnTurnStarted += HandleTurnChanged;
            if (TurnControl.Instance != null)
            {
                PlayerMovement currentPlayer = TurnControl.Instance.GetCurrentActivePlayer();
                if (currentPlayer != null)
                {
                    HandleTurnChanged(currentPlayer);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TurnControl.OnTurnStarted -= HandleTurnChanged;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetLinkedPlayerServerRpc(ulong playerId)
    {
        linkedPlayerId.Value = playerId;
        if (TurnControl.Instance != null)
        {
            PlayerMovement currentPlayer = TurnControl.Instance.GetCurrentActivePlayer();
            if (currentPlayer != null)
            {
                HandleTurnChanged(currentPlayer);
            }
        }
    }

    private void HandleTurnChanged(PlayerMovement newActivePlayer)
    {
        if (!IsServer) return;
        if (newActivePlayer == null) return;
        if (linkedPlayerId.Value == 99) return;

        ulong newActivePlayerId = newActivePlayer.OwnerClientId;

        if (newActivePlayerId == linkedPlayerId.Value)
        {
            canPursue = false;
            targetPlayerTransform = null;
            targetPlayerState = null;
            currentState.Value = EnemyState.Idle;
        }
        else
        {
            canPursue = true;
            PlayerMovement linkedPlayerMovement = TurnControl.Instance.players
                .FirstOrDefault(p => p.OwnerClientId == linkedPlayerId.Value);

            if (linkedPlayerMovement != null)
            {
                targetPlayerTransform = linkedPlayerMovement.transform;
                targetPlayerState = linkedPlayerMovement.GetComponent<PlayerState>();

                if (targetPlayerState == null)
                {
                    Debug.LogError($"[EnemyController] Jogador {linkedPlayerId.Value} não tem um script PlayerState.cs!");
                    canPursue = false;
                    currentState.Value = EnemyState.Idle;
                }
                else
                {
                    currentState.Value = EnemyState.Pursuing;
                }
            }
            else
            {
                Debug.LogError($"[EnemyController] Não encontrou o PlayerMovement com ID {linkedPlayerId.Value}");
                targetPlayerTransform = null;
                targetPlayerState = null;
                currentState.Value = EnemyState.Idle;
            }
        }
    }

    private void Update()
    {
        // --- MUDANÇA 2: Cliente não deve animar ---
        // A lógica de animação foi movida para dentro do 'if (IsServer)'
        if (!IsServer)
        {
            // O Cliente não faz nada no Update.
            // O NetworkTransform move o inimigo.
            // O NetworkAnimator anima o inimigo.
            return;
        }

        if (!canPursue || targetPlayerTransform == null || currentState.Value == EnemyState.Attacking)
        {
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            if (IsPlayerOnSafePlatform())
            {
                currentState.Value = EnemyState.Waiting;
            }
            else if (currentState.Value == EnemyState.Waiting)
            {
                currentState.Value = EnemyState.Pursuing;
            }

            switch (currentState.Value)
            {
                case EnemyState.Pursuing:
                    MoveTowardsPlayer();
                    break;
                case EnemyState.Waiting:
                    rb.linearVelocity = Vector2.zero;
                    break;
                case EnemyState.Idle:
                    rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        // --- MUDANÇA 3: Animação movida ---
        // Apenas o Servidor decide se o inimigo está se movendo.
        // O NetworkAnimator sincronizará o bool "IsMoving" para os clientes.
        UpdateAnimations();
    }

    private bool IsPlayerOnSafePlatform()
    {
        if (targetPlayerState != null)
        {
            return targetPlayerState.IsOnSafePlatform.Value;
        }
        return false;
    }

    private void MoveTowardsPlayer()
    {
        Vector2 direction = (targetPlayerTransform.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        if (direction.x > 0.01f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void UpdateAnimations()
    {
        bool isMoving = currentState.Value == EnemyState.Pursuing;
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        if (currentState.Value != EnemyState.Pursuing && currentState.Value != EnemyState.Waiting) return;
        if (other.transform != targetPlayerTransform) return;

        if (other.CompareTag("Player"))
        {
            StartAttack();
        }
    }

    private void StartAttack()
    {
        if (!IsServer) return;
        currentState.Value = EnemyState.Attacking;
        rb.linearVelocity = Vector2.zero;

        // Desativa as partículas em todos os clientes
        DisableParticlesClientRpc();

        // Aplica o debuff (lógica do servidor, está correto)
        PlayerMovement linkedPlayerMovement = TurnControl.Instance.players
            .FirstOrDefault(p => p.OwnerClientId == linkedPlayerId.Value);

        if (linkedPlayerMovement != null)
        {
            DebuffVisionControl debuffControl = linkedPlayerMovement.GetComponent<DebuffVisionControl>();
            if (debuffControl != null)
            {
                debuffControl.StartDebuffServer(debuffDurationTurns, linkedPlayerId.Value);
            }
        }

        // --- MUDANÇA 4: O CONSERTO PRINCIPAL ---
        // REMOVEMOS O RPC de ataque.
        // O Servidor toca a animação NELE MESMO.
        // O NetworkAnimator vai ver isso e sincronizar com TODOS os clientes.
        animator.SetTrigger("Attack");
        // --------------------------------------

        StartCoroutine(AttackCooldown());
    }

    // --- MUDANÇA 5: RPC REMOVIDO ---
    // Este método era a causa do bug e não é mais necessário.
    // [ClientRpc]
    // private void AttackClientRpc(ClientRpcParams rpcParams = default)
    // {
    //     animator.SetTrigger("Attack");
    // }
    // ---------------------------------

    [ClientRpc]
    private void DisableParticlesClientRpc()
    {
        if (particleEffects == null || particleEffects.Length == 0)
        {
            Debug.LogWarning("[EnemyController] Nenhum efeito de partícula foi atribuído no Inspector.");
            return;
        }

        foreach (GameObject particle in particleEffects)
        {
            if (particle != null)
            {
                particle.SetActive(false);
            }
        }
        Debug.Log($"[EnemyController] {particleEffects.Length} sistemas de partículas desativados em todos os clientes.");
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(1.5f);
        if (IsServer)
        {
            NetworkObject.Despawn(gameObject);
        }
    }
}