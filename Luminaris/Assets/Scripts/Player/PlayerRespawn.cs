using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;
    [SerializeField] private GameObject feedBackTextualPrefab;

    private Vector3 respawnPoint;
    private Transform lastCheckpoint;
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
            var checkpoint = collision.GetComponent<Checkpoint>();
            if (checkpoint != null && collision.transform != lastCheckpoint)
            {
                lastCheckpoint = collision.transform;
                respawnPoint = checkpoint.RespawnPosition;

                // Só mostra feedback se for a primeira vez que ativa este checkpoint
                if (checkpoint.TryActivate())
                {
                    GameManager.Instance.ReachCheckpoint(collision.transform);
                    ShowFeedback("Checkpoint salvo!", collision.transform.position + Vector3.up * 1.25f);
                }
                else
                {
                    // ainda atualiza o save, mas sem feedback
                    GameManager.Instance.ReachCheckpoint(collision.transform);
                }
            }
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        movementScript.EndTurn();
        TurnControl.Instance.EndTurnIfReady();

        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        transform.position = respawnPoint;
        movementScript.StartTurn();
        isDead = false;
    }

    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);
        var textComp = temp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }
}
