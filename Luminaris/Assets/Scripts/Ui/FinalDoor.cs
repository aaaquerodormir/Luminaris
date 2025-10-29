using UnityEngine;
using Unity.Netcode;

public class FinalDoor : NetworkBehaviour
{
    // Campo que você configura no Inspector (TEMPLATES estáticos)
    [Header("Player Template (Apenas para o Spawner identificar)")]
    [SerializeField] private PlayerRespawn assignedPlayerTemplate;

    // NOVO: O ID REAL do cliente que esta porta está esperando.
    // É setado em runtime pelo Spawner, não pelo Inspector.
    private ulong targetClientId = ulong.MaxValue;

    public NetworkVariable<bool> playerInside = new NetworkVariable<bool>(false);

    public bool IsPlayerInside => playerInside.Value;

    // NOVO MÉTODO: Chamado APENAS pelo Spawner (Servidor)
    public void SetTargetClientId(ulong clientId)
    {
        if (!IsServer) return;
        targetClientId = clientId;
        Debug.Log($"[FinalDoor] Definido Target Client ID: {clientId} (Porta: {gameObject.name})");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (!collision.CompareTag("Player")) return;

        // Se o targetClientId ainda não foi setado pelo Spawner, ignora.
        if (targetClientId == ulong.MaxValue) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        // Lógica "Procurar": A porta verifica se o objeto que colidiu é de fato
        // o Player Object que tem o OwnerClientId que ela espera.
            //LevelManager.Instance.CheckForAllPlayersReady();
        if (respawn.OwnerClientId == targetClientId)
        {
            //LevelManager.Instance.PlayerDoorCount.Value++;
            playerInside.Value = true;
            Debug.Log($"[FinalDoor] Player ID {targetClientId} (OwnerClientId) ENTROU.");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (!collision.CompareTag("Player")) return;

        if (targetClientId == ulong.MaxValue) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        if (respawn.OwnerClientId == targetClientId)
        {
            //LevelManager.Instance.PlayerDoorCount.Value++;
            playerInside.Value = false;
            Debug.Log($"[FinalDoor] Player ID {targetClientId} SAIU.");
        }
    }
}