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

    [Header("Configurações de Animação")]
    [Tooltip("Quanto tempo (em segundos) dura a animação de spawn.")]
    [SerializeField]
    private float spawnDuration = 1.5f;

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
            animator.SetTrigger("Spawn");
            StartCoroutine(FinishSpawning());
            TurnControl.OnTurnStarted += HandleTurnChanged;
        }
    }

    // --- MUDANÇA CRÍTICA AQUI ---
    private IEnumerator FinishSpawning()
    {
        yield return new WaitForSeconds(spawnDuration);

        if (IsServer)
        {
            Debug.Log("[EnemyController] Spawn completo. Mudando para estado Idle.");

            // 1. Tira o inimigo do estado 'Spawning'
            //    Isso o "desbloqueia".
            currentState.Value = EnemyState.Idle;

            // 2. AGORA, chama HandleTurnChanged para definir o alvo.
            //    Como o estado não é mais 'Spawning', a função vai rodar.
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
    // --- FIM DA MUDANÇA ---

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

        if (currentState.Value == EnemyState.Spawning)
        {
            Debug.Log("[EnemyController] Jogador linkado, aguardando spawn terminar.");
            return;
        }

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

        // Esta trava ainda é importante, caso o turno mude
        // ANTES da corrotina FinishSpawning terminar.
        if (currentState.Value == EnemyState.Spawning)
        {
            Debug.Log("[EnemyController] Troca de turno ignorada (ainda em spawn).");
            return;
        }

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
                targetPlayerTransform = null;
                targetPlayerState = null;
                currentState.Value = EnemyState.Idle;
            }
        }
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!canPursue || targetPlayerTransform == null ||
            currentState.Value == EnemyState.Attacking ||
            currentState.Value == EnemyState.Spawning)
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
                case EnemyState.Idle:
                    rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

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
        if (currentState.Value == EnemyState.Spawning) return;
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

        DisableParticlesClientRpc();

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

        animator.SetTrigger("Attack");

        StartCoroutine(AttackCooldown());
    }

    [ClientRpc]
    private void DisableParticlesClientRpc()
    {
        if (particleEffects == null || particleEffects.Length == 0) return;

        foreach (GameObject particle in particleEffects)
        {
            if (particle != null)
            {
                particle.SetActive(false);
            }
        }
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