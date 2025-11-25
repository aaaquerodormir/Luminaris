using UnityEngine;
using System;
using Unity.Netcode;

public class PlayerRespawn : NetworkBehaviour
{
    public static event Action OnPlayerDied;

    [SerializeField] private PlayerMovement movementScript;
    [SerializeField] private GameObject feedBackTextualPrefab;

    private Vector3 respawnPoint;
    private Checkpoint committedCheckpoint;
    private Checkpoint pendingCheckpoint;
    private bool isDead = false;

    private void Start()
    {
        respawnPoint = transform.position;
        Debug.Log($"[PlayerRespawn] Iniciado no jogador {OwnerClientId}, posição inicial: {respawnPoint}");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return; // Apenas o servidor processa mortes e checkpoints

        if (collision.CompareTag("Lava") && !isDead)
        {
            Debug.Log($"[PlayerRespawn] Jogador {OwnerClientId} caiu na lava!");
            DieServerRpc();
            return;
        }

        if (collision.CompareTag("Checkpoint"))
        {
            var checkpoint = collision.GetComponent<Checkpoint>();
            if (checkpoint == null) return;

            if (pendingCheckpoint == checkpoint || committedCheckpoint == checkpoint) return;

            pendingCheckpoint = checkpoint;
            //checkpoint.TryActivate();
            //GameManager.Instance.ReachCheckpoint(checkpoint.transform);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DieServerRpc()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[PlayerRespawn] Jogador {OwnerClientId} morreu. Notificando clientes...");
        DieClientRpc();

        OnPlayerDied?.Invoke(); // Notifica GameManager no servidor
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        AudioManager.Instance.PlaySound("Morrendo");
    }

    public void Respawn()
    {
        if (!IsServer) return;

        Debug.Log($"[PlayerRespawn] Respawn do jogador {OwnerClientId} em {respawnPoint}");
        transform.position = respawnPoint;
        isDead = false;

        RespawnClientRpc(respawnPoint);
    }

    [ClientRpc]
    private void RespawnClientRpc(Vector3 pos)
    {
        transform.position = pos;
        isDead = false;

        if (movementScript != null)
            //movementScript.StartTurn();

        Debug.Log($"[PlayerRespawn] Cliente reposicionado para {pos}");
    }

    public void CommitPendingCheckpoint()
    {
        if (pendingCheckpoint == null) return;

        committedCheckpoint = pendingCheckpoint;
        respawnPoint = committedCheckpoint.RespawnPosition;
        ShowFeedback("Checkpoint salvo", committedCheckpoint.transform.position + Vector3.up * 1.25f);
        pendingCheckpoint = null;
    }

    public Checkpoint GetPendingCheckpoint() => pendingCheckpoint;
    public Checkpoint GetCommittedCheckpoint() => committedCheckpoint;

    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);
        var textComp = temp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null) textComp.text = mensagem;

        Destroy(temp, 1.5f);
    }
}
