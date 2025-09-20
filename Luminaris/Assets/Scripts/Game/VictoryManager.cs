using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    private static VictoryManager instance;
    private FinalDoor[] doors;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        doors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
        Debug.Log($"[VictoryManager] Portas encontradas: {doors.Length}");
    }

    public static void CheckVictory()
    {
        if (instance == null) return;

        foreach (var door in instance.doors)
        {
            if (door == null || !door.IsPlayerInside)
                return; // algum jogador ainda não chegou
        }

        Debug.Log("[VictoryManager] Todos os jogadores estão nas portas!");
        GameManager.Instance.ShowVictoryPanel();

    }
}
