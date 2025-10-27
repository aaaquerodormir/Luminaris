using UnityEngine;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlataformaEspinhos : NetworkBehaviour
{
    [Header("Configuração de Timing (Somente Host)")]

    [SerializeField]
    [Tooltip("O atraso inicial (em segundos) antes do primeiro ciclo de ataque começar.")]
    private float initialDelay = 3.0f;

    [SerializeField]
    [Tooltip("A duração (em segundos) que o espinho fica em 'Idle' (abaixado) entre os ataques.")]
    private float pauseDuration = 5.0f;


    [Header("Configuração de Animação")]

    [SerializeField]
    [Tooltip("O NOME EXATO do clipe de animação de 'Ataque' no seu Animator Controller. Usado para buscar sua duração.")]
    private string attackAnimationName = "Espinhos Ativados";

    [SerializeField]
    [Tooltip("O NOME EXATO do parâmetro 'Trigger' no seu Animator Controller que inicia o ataque.")]
    private string activateTriggerName = "Activate";

    private bool m_IsLethal = false;

    // Componentes cacheados
    private Animator m_Animator;

    // Estado interno
    private float m_AttackAnimationLength = 1.0f; // Duração do clipe de ataque (será buscada)

    /// <summary>
    /// Awake é chamado quando o script é carregado.
    /// Usamos para cachear componentes e buscar a duração da animação.
    /// </summary>
    private void Awake()
    {
        // Cacheia o componente Animator local
        m_Animator = GetComponent<Animator>();

        // Busca a duração real da animação de ataque para um timing perfeito
        FindAttackAnimationLength();
    }

    /// <summary>
    /// Busca a duração do clipe de animação de ataque pelo nome.
    /// </summary>
    private void FindAttackAnimationLength()
    {
        if (m_Animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"[SpikePlatform] O Animator neste objeto ({gameObject.name}) não possui um RuntimeAnimatorController assignado.", this);
            return;
        }

        // Itera por todos os clipes no controller
        foreach (var clip in m_Animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == attackAnimationName)
            {
                m_AttackAnimationLength = clip.length;
                return; // Encontramos o clipe e armazenamos sua duração
            }
        }

        // Aviso caso o clipe não seja encontrado
        Debug.LogWarning($"[SpikePlatform] Não foi possível encontrar o clipe de animação com o nome '{attackAnimationName}'. " +
                         $"Usando a duração padrão de {m_AttackAnimationLength}s. " +
                         $"Verifique o campo 'Attack Animation Name' no Inspector.", this);
    }

    /// <summary>
    /// OnNetworkSpawn é chamado quando o objeto é spawnado na rede.
    /// É a maneira correta de iniciar lógicas de rede.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        // Esta lógica DEVE rodar apenas no Servidor (Host).
        // O Host tem autoridade sobre o "quando" o espinho ataca.
        if (IsServer)
        {
            // Inicia a corrotina que gerencia o ciclo de ataque/pausa.
            StartCoroutine(SpikeCycleCoroutine());
        }

        // NOTA SOBRE RACE CONDITION:
        // Não precisamos de lógica extra no cliente. Se um cliente entrar
        // no meio de uma animação, o NetworkAnimator automaticamente sincronizará
        // o estado atual (ex: "Attack" em 50% de progresso). O servidor
        // continua seu ciclo de timer, e o próximo trigger "Activate" será
        // enviado e sincronizado normalmente.
    }

    // Novo método para detectar a colisão
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Apenas o SERVIDOR deve processar a colisão.
        // 2. Só aciona a morte se o espinho estiver atualmente letal (levantado).
        if (!IsServer || !m_IsLethal) return;

        // Verifica se o objeto que colidiu é o jogador (assumindo a tag "Player")
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[PlataformaEspinhos-SERVER] Jogador {other.gameObject.name} atingido! Acionando Game Over.");

            // Chama a função ServerRpc do GameManager para notificar o servidor sobre a morte.
            // O GameManager então enviará o ClientRpc de Game Over para todos.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDiedServerRpc();
            }
            else
            {
                Debug.LogError("[PlataformaEspinhos] GameManager.Instance é nulo. A morte não pode ser acionada.");
            }

            // Define como não-letal imediatamente para evitar múltiplas chamadas
            // de Game Over se o jogador ficar no collider durante a pausa.
            m_IsLethal = false;
        }
    }
    private IEnumerator SpikeCycleCoroutine()
    {
        // 1. Espera o delay inicial configurável
        yield return new WaitForSeconds(initialDelay);

        // Loop infinito enquanto formos o servidor
        while (IsServer)
        {
            // 2. Dispara o gatilho de ataque.
            m_Animator.SetTrigger(activateTriggerName);

            // >>> INÍCIO DO PERÍODO LETAL (espinho está subindo) <<<
            m_IsLethal = true;

            // 3. Espera a duração da animação de ATAQUE
            yield return new WaitForSeconds(m_AttackAnimationLength);

            // >>> FIM DO PERÍODO LETAL (espinho está abaixado) <<<
            m_IsLethal = false;

            // 4. Espera a duração da PAUSA (estado Idle)
            yield return new WaitForSeconds(pauseDuration);
        }
    }
}
