using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    // Evento estático para notificar quando um jogador morreu
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;

    [Header("Feedback")]
    [SerializeField] private GameObject feedBackTextualPrefab;

    private Vector3 respawnPoint;     // ponto salvo para respawn
    private Transform lastCheckpoint; // checkpoint mais recente
    private bool isDead = false;      // flag de morte para evitar múltiplas execuções

    void Start()
    {
        // O ponto inicial de respawn é a posição inicial do jogador
        respawnPoint = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se tocar na lava → morre
        if (collision.CompareTag("Lava") && !isDead)
        {
            Die();
        }
        // Se tocar em checkpoint → atualiza posição de respawn
        else if (collision.CompareTag("Checkpoint"))
        {
            // só atualiza se for um checkpoint novo
            if (collision.transform != lastCheckpoint)
            {
                lastCheckpoint = collision.transform;
                respawnPoint = collision.transform.position;

                // mostra feedback em cima do checkpoint
                ShowFeedback("Checkpoint salvo!", collision.transform.position + Vector3.up * 1.25f);
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Desativa movimentação do jogador
        movementScript.EndTurn();

        //Força fim de turno se um jogador morrer
        TurnControl.Instance.EndTurnIfReady();

        // Notifica o GameManager
        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        // Reposiciona no último checkpoint salvo
        transform.position = respawnPoint;

        // Reativa movimentação
        movementScript.StartTurn();
        isDead = false;
    }

    //Método auxiliar para mostrar feedback textual
    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        // Instancia o prefab na posição do checkpoint (levemente acima)
        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);

        // Se o prefab tiver um componente de texto, muda a mensagem
        var textComp = temp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }
}
