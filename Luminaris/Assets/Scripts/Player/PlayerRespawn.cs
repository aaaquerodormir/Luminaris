using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    // Evento estático para notificar quando um jogador morreu
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;

    private Vector3 respawnPoint; // ponto salvo para respawn
    private bool isDead = false;  // flag de morte para evitar múltiplas execuções

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
            respawnPoint = collision.transform.position;
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Desativa movimentação do jogador
        movementScript.EndTurn();

        // 🔥 Força fim de turno se um jogador morrer
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
}
