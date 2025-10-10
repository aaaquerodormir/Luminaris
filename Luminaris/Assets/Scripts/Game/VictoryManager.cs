using UnityEngine;
using Unity.Netcode;
public class VictoryManager : NetworkBehaviour
{
    private static VictoryManager instance;
    private FinalDoor[] doors;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        doors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
        Debug.Log($"[VictoryManager] Encontradas {doors.Length} portas finais.");
    }

    public static void CheckVictory()
    {
        if (instance == null) return;
        if (!instance.IsServer)
        {
            Debug.Log("[VictoryManager] Cliente tentou verificar vitória — ignorado.");
            return;
        }

        foreach (var door in instance.doors)
        {
            if (door == null || !door.IsPlayerInside)
                return;
        }

        Debug.Log("[VictoryManager] Todos os jogadores chegaram! Enviando RPC de vitória.");
        instance.NotifyVictoryClientRpc();
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        Debug.Log("[VictoryManager] RPC de vitória recebido — exibindo painel.");
        GameManager.Instance.ShowVictoryPanelClientRpc();
    }

    //public void OnClickMenuPrincipal()
    //{
    //    //GameManager.Instance.OpenVictoryConfirmation(() =>
    //    {
    //        Time.timeScale = 1f;
    //        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");

    //    );
    //    }
    //}
}