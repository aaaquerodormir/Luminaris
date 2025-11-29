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
    private Animator m_Animator;

    private float m_AttackAnimationLength = 1.0f;
    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
        FindAttackAnimationLength();
    }

    private void FindAttackAnimationLength()
    {
        if (m_Animator.runtimeAnimatorController == null)
        {
            return;
        }
        foreach (var clip in m_Animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == attackAnimationName)
            {
                m_AttackAnimationLength = clip.length;
                return;
            }
        }

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(SpikeCycleCoroutine());
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer || !m_IsLethal) return;

        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDiedServerRpc();
            }
            else
            {
            }
            m_IsLethal = false;
        }
    }
    private IEnumerator SpikeCycleCoroutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (IsServer)
        {
            m_Animator.SetTrigger(activateTriggerName);
            m_IsLethal = true;
            yield return new WaitForSeconds(m_AttackAnimationLength);
            m_IsLethal = false;
            yield return new WaitForSeconds(pauseDuration);
        }
    }
}
