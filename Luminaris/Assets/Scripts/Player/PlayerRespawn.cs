using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;

    private Vector3 respawnPoint;
    private bool isDead = false;

    void Start()
    {
        respawnPoint = transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Lava") && !isDead)
        {
            Die();
        }
        else if (collision.CompareTag("Checkpoint"))
        {
            respawnPoint = collision.transform.position;
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        movementScript.EndTurn();

        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        transform.position = respawnPoint;
        movementScript.StartTurn();
        isDead = false;
    }
}
