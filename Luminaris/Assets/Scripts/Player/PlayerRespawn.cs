using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    // Evento estático para notificar quando um jogador morrer
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;

    private Vector3 respawnPoint; // Ponto atual de respawn do jogador
    private bool isDead = false;  // Flag para evitar mortes repetidas

    void Start()
    {
        // Define o ponto inicial de respawn como a posição inicial
        respawnPoint = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se tocar na lava → morre
        if (collision.CompareTag("Lava") && !isDead)
        {
            Die();
        }
        // Se tocar em um checkpoint → salva novo ponto de respawn
        else if (collision.CompareTag("Checkpoint"))
        {
            respawnPoint = collision.transform.position;
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // Finaliza o turno ao morrer
        movementScript.EndTurn();

        // Dispara evento para o GameManager reagir
        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        // Reposiciona no último checkpoint
        transform.position = respawnPoint;

        // Reativa o jogador
        movementScript.StartTurn();
        isDead = false;
    }
}
