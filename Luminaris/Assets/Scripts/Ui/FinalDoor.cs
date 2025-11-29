using UnityEngine;
using Unity.Netcode;

public class FinalDoor : NetworkBehaviour
{
    [Header("Player Template (Apenas para o Spawner identificar)")]
    [SerializeField] private PlayerRespawn assignedPlayerTemplate;

    private ulong targetClientId = ulong.MaxValue;

    public NetworkVariable<bool> playerInside = new NetworkVariable<bool>(false);

    public bool IsPlayerInside => playerInside.Value;
    public void SetTargetClientId(ulong clientId)
    {
        if (!IsServer) return;
        targetClientId = clientId;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (!collision.CompareTag("Player")) return;

        if (targetClientId == ulong.MaxValue) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        if (respawn.OwnerClientId == targetClientId)
        {
            playerInside.Value = true;
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
            playerInside.Value = false;
        }
    }
}