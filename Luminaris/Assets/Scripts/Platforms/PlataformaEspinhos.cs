using UnityEngine;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlataformaEspinhos : NetworkBehaviour
{
    [SerializeField] private float pauseDuration = 2f;   // Tempo total do ciclo (sobe + desce)
    [SerializeField] private float initialDelay = 0f;    // Atraso inicial
    [SerializeField] private float detectionRadius = 6f; // Raio para ouvir o som
    [SerializeField] private LayerMask playerMask;       // Layer dos jogadores

    private Animator animator;
    private NetworkAnimator netAnimator;
    private Transform espinhoTransform;

    private static readonly int ActivateTrigger = Animator.StringToHash("Activate");
    // 🔑 NOVO: Hash para o Trigger de Saída (Stop)
    private static readonly int StopTrigger = Animator.StringToHash("Stop");

    private void Awake()
    {
        espinhoTransform = transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 1. Obter componentes no OnNetworkSpawn continua correto.
        animator = GetComponent<Animator>();
        netAnimator = GetComponent<NetworkAnimator>();

        if (netAnimator == null)
        {
            Debug.LogError("NetworkAnimator não encontrado após OnNetworkSpawn. O objeto pode estar mal configurado. A desativação é segura se não for o objeto host.", this);
            // Retornar aqui é seguro para clientes sem a autoridade ou configuração.
            return;
        }

        if (IsServer)
        {
            // 2. CORREÇÃO FINAL: Esperar 1 frame antes de iniciar a lógica do ciclo.
            // Isso garante que o netAnimator está 100% pronto.
            StartCoroutine(AwaitForReadinessAndStartCycle());
        }
    }

    // 🔑 NOVO: Controla a pausa (o tempo que a plataforma fica inativa)
    private IEnumerator AwaitForReadinessAndStartCycle()
    {
        // Espera o próximo ciclo de atualização do Unity (próximo frame).
        yield return null;

        // Agora, o Servidor inicia o ciclo de PAUSA com o delay inicial.
        StartCoroutine(StartNewCycle(initialDelay));
    }

    private IEnumerator StartNewCycle(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 1. A PAUSA terminou. Agora inicia o ATAQUE.
        TriggerAttack();
    }

    // 🔑 NOVO: Função chamada pelo Animation Event no final do ataque!
    // Esta função reinicia o ciclo de PAUSA.
    public void EndAttackEvent()
    {
        if (!IsServer) return;

        // 1. Dispara o Trigger 'Stop' para que o Animator saia
        // do estado 'Espinhos Ativados' e entre em 'Espinhos Desativados' (Idle).
        netAnimator.SetTrigger(StopTrigger);

        // 2. Inicia o timer de PAUSA (o controle do Inspector).
        StartCoroutine(StartNewCycle(pauseDuration));
    }

    // Dispara o Trigger de ataque pela rede.
    private void TriggerAttack()
    {
        if (!IsServer) return;

        // Dispara o Trigger 'Activate' para iniciar o ataque.
        netAnimator.SetTrigger(ActivateTrigger);

        if (IsPlayerNearby())
        {
            PlaySpikeSoundClientRpc();
        }
    }

    // Restante do código (ClientRpc, Colisão, Gizmos) permanece igual...

    [ClientRpc]
    private void PlaySpikeSoundClientRpc()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("Espinho");
        }
    }

    private bool IsPlayerNearby()
    {
        Collider2D playerNearby = Physics2D.OverlapCircle(
            espinhoTransform.position, detectionRadius, playerMask
        );
        return playerNearby != null;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (IsServer)
        {
            var respawn = collision.GetComponentInParent<PlayerRespawn>();
            // ... (lógica de dano)
            HandlePlayerBounce(collision.attachedRigidbody);
        }
    }

    private void HandlePlayerBounce(Rigidbody2D rb)
    {
        if (rb == null) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
