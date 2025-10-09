using UnityEngine;
using System;
//using Unity.Netcode;

public class PlayerRespawn : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;
    [SerializeField] private GameObject feedBackTextualPrefab;

    private Vector3 respawnPoint;
    private Checkpoint committedCheckpoint;
    private Checkpoint pendingCheckpoint;
    private bool isDead = false;

    void Start()
    {
        respawnPoint = transform.position;
        Debug.Log($"[PlayerRespawn] Ponto inicial de respawn definido em {respawnPoint}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Lava") && !isDead)
        {
            Debug.Log("[PlayerRespawn] Jogador tocou na Lava — morrendo.");
            Die();
            return;
        }

        if (!collision.CompareTag("Checkpoint")) return;

        var checkpoint = collision.GetComponent<Checkpoint>();
        if (checkpoint == null) return;

        if (pendingCheckpoint == checkpoint || committedCheckpoint == checkpoint) return;

        Debug.Log($"[PlayerRespawn] Novo checkpoint detectado: {checkpoint.name}");
        pendingCheckpoint = checkpoint;
        checkpoint.TryActivate();
        GameManager.Instance.ReachCheckpoint(checkpoint.transform);
    }

    public Checkpoint GetPendingCheckpoint() => pendingCheckpoint;
    public Checkpoint GetCommittedCheckpoint() => committedCheckpoint;

    public void CommitPendingCheckpoint()
    {
        if (pendingCheckpoint == null) return;
        committedCheckpoint = pendingCheckpoint;
        respawnPoint = committedCheckpoint.RespawnPosition;
        ShowFeedback("Checkpoint salvo", committedCheckpoint.transform.position + Vector3.up * 1.25f);
        Debug.Log($"[PlayerRespawn] Checkpoint confirmado: {committedCheckpoint.name}");
        pendingCheckpoint = null;
    }

    public void ClearPendingCheckpoint() => pendingCheckpoint = null;

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        ClearPendingCheckpoint();

        Debug.Log("[PlayerRespawn] Executando sequência de morte...");

        if (movementScript != null)
            movementScript.EndTurn();

        TurnControl.Instance?.EndTurnIfReady();

        AudioManager.Instance.PlaySound("Morrendo");
        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        Debug.Log($"[PlayerRespawn] Respawn em {respawnPoint}");
        transform.position = respawnPoint;

        if (movementScript != null)
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
