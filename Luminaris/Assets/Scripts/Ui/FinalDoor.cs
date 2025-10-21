using UnityEngine;
using Unity.Netcode;

public class FinalDoor : NetworkBehaviour
{
    [Header("Jogador esperado nesta porta")]
    [SerializeField] private PlayerRespawn assignedPlayer;

    private bool playerInside = false;

    public bool IsPlayerInside => playerInside;
    public PlayerRespawn AssignedPlayer => assignedPlayer;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        if (respawn == assignedPlayer)
        {
            playerInside = true;
            Debug.Log($"[FinalDoor] {assignedPlayer.name} entrou na porta!");
            VictoryManager.CheckVictory(); // ✅ já chama corretamente
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var respawn = collision.GetComponentInParent<PlayerRespawn>();
        if (respawn == null) return;

        if (respawn == assignedPlayer)
        {
            playerInside = false;
            Debug.Log($"[FinalDoor] {assignedPlayer.name} saiu da porta.");
        }
    }
}
