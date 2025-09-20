using UnityEngine;

public class FinalDoor : MonoBehaviour
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
            Debug.Log($"[FinalDoor] {respawn.name} entrou na porta {name}");
            VictoryManager.CheckVictory();
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
            Debug.Log($"[FinalDoor] {respawn.name} saiu da porta {name}");
        }
    }
}
