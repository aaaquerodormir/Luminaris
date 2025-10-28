using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq; // Necessário para .FirstOrDefault

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

    [Header("Referências")]
    [SerializeField]
    private Animator animator;
    private Rigidbody2D rb;

    private NetworkVariable<EnemyState> currentState = new NetworkVariable<EnemyState>(EnemyState.Spawning);
    private NetworkVariable<ulong> linkedPlayerId = new NetworkVariable<ulong>(99); // 99 = ID inválido

    private Transform targetPlayerTransform; // O transform do jogador a ser perseguido
    private PlayerState targetPlayerState; // Referência ao script PlayerState do jogador
    private bool canPursue = false;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (IsServer)
        {
            // Se inscreve no evento OnTurnStarted do SEU TurnControl
            TurnControl.OnTurnStarted += HandleTurnChanged;

            // Caso o inimigo seja spownado no meio de um turno, verifica o estado
            if (TurnControl.Instance != null)
            {
                // Usa o método helper para pegar o jogador ativo
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

    // Chamado pelo TrapPlatform (ServerRpc) para definir o "dono"
    [ServerRpc(RequireOwnership = false)]
    public void SetLinkedPlayerServerRpc(ulong playerId)
    {
        linkedPlayerId.Value = playerId;
        // Atualiza o estado imediatamente após ser definido
        if (TurnControl.Instance != null)
        {
            PlayerMovement currentPlayer = TurnControl.Instance.GetCurrentActivePlayer();
            if (currentPlayer != null)
            {
                HandleTurnChanged(currentPlayer);
            }
        }
    }

    // Lógica central: O inimigo reage à troca de turno
    private void HandleTurnChanged(PlayerMovement newActivePlayer)
    {
        if (!IsServer) return;
        if (newActivePlayer == null) return;

        if (linkedPlayerId.Value == 99) return; // Inimigo não foi configurado

        ulong newActivePlayerId = newActivePlayer.OwnerClientId;

        // Se o turno ATUAL é o do jogador "linkado"...
        if (newActivePlayerId == linkedPlayerId.Value)
        {
            // ...o inimigo FICA PARADO.
            canPursue = false;
            targetPlayerTransform = null;
            targetPlayerState = null; // Limpa a referência
            currentState.Value = EnemyState.Idle;
        }
        else // Se for o turno do OUTRO jogador...
        {
            // ...o inimigo PERSEGUE o seu jogador "linkado" (que está inativo)
            canPursue = true;

            // Busca o PlayerMovement do jogador "linkado" na lista do TurnControl
            PlayerMovement linkedPlayerMovement = TurnControl.Instance.players
                .FirstOrDefault(p => p.OwnerClientId == linkedPlayerId.Value);

            if (linkedPlayerMovement != null)
            {
                // Pega o transform e o script PlayerState
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
        if (!IsServer)
        {
            UpdateAnimations();
            return;
        }

        if (!canPursue || targetPlayerTransform == null || currentState.Value == EnemyState.Attacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Verifica se o jogador alvo está na plataforma segura
        if (IsPlayerOnSafePlatform())
        {
            currentState.Value = EnemyState.Waiting;
        }
        else if (currentState.Value == EnemyState.Waiting) // Se saiu da plataforma segura
        {
            currentState.Value = EnemyState.Pursuing;
        }

        // Executa ação do estado
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
        // *** CORREÇÃO UNITY 6 ***
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
        // Verifica se o estado permite atacar/tocar
        if (currentState.Value != EnemyState.Pursuing && currentState.Value != EnemyState.Waiting) return;

        // Verifica se o objeto que entrou no Trigger é o jogador alvo
        if (other.transform != targetPlayerTransform) return;

        // Garante que é o jogador e não um collider extra
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
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { linkedPlayerId.Value }
            }
        };
        AttackClientRpc(rpcParams);
        StartCoroutine(AttackCooldown());
    }

    [ClientRpc]
    private void AttackClientRpc(ClientRpcParams rpcParams = default)
    {
        // Este código agora só será executado no cliente cujo ID foi especificado em rpcParams
        animator.SetTrigger("Attack");

        if (ScreenFade.Instance != null)
        {
            ScreenFade.Instance.StartFade();
        }
    }

    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(1.5f);
        currentState.Value = EnemyState.Idle;
        canPursue = false;
    }
}