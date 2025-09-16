using UnityEngine;
using System;

public class PlayerRespawn : MonoBehaviour
{
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;
    [SerializeField] private GameObject feedBackTextualPrefab;

    private Vector3 respawnPoint;
    private Checkpoint committedCheckpoint; // último checkpoint realmente comitado para este jogador
    private Checkpoint pendingCheckpoint;   // último checkpoint tocado mas ainda não sincronizado
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
            return;
        }

        if (!collision.CompareTag("Checkpoint")) return;

        var checkpoint = collision.GetComponent<Checkpoint>();
        if (checkpoint == null) return;

        // se já é o mesmo pending ou já comitado, nada a fazer
        if (pendingCheckpoint == checkpoint || committedCheckpoint == checkpoint) return;

        pendingCheckpoint = checkpoint;

        // ativa a visual do checkpoint (a persistência só ocorrerá quando ambos sincronizarem)
        checkpoint.TryActivate();

        // avisa o GameManager para verificar sincronização (GameManager decide comitar)
        GameManager.Instance.ReachCheckpoint(checkpoint.transform);
    }

    public Checkpoint GetPendingCheckpoint()
    {
        return pendingCheckpoint;
    }

    public Checkpoint GetCommittedCheckpoint()
    {
        return committedCheckpoint;
    }

    // Chamado pelo GameManager quando a sincronização do grupo é confirmada
    public void CommitPendingCheckpoint()
    {
        if (pendingCheckpoint == null) return;
        committedCheckpoint = pendingCheckpoint;
        respawnPoint = committedCheckpoint.RespawnPosition;
        ShowFeedback("Checkpoint salvo!", committedCheckpoint.transform.position + Vector3.up * 1.25f);
        pendingCheckpoint = null;
    }

    public void ClearPendingCheckpoint()
    {
        pendingCheckpoint = null;
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        // limpa pending se morrer antes da sincronização
        ClearPendingCheckpoint();

        // encerra o turno deste jogador e avisa TurnControl
        if (movementScript != null)
            movementScript.EndTurn();

        TurnControl.Instance.EndTurnIfReady();

        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
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
