using UnityEngine;
using Unity.Netcode;
public class VictoryManager : NetworkBehaviour
{
    private static VictoryManager Instance;
    private FinalDoor[] doors;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        doors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
        Debug.Log($"[VictoryManager] Encontradas {doors.Length} portas finais.");
    }

    /// <summary>
    /// Chamado pelas portas quando um jogador entra.
    /// </summary>
    public static void CheckVictory()
    {
        if (Instance == null) return;

        // Somente o servidor valida vitória
        if (!Instance.IsServer)
        {
            Debug.Log("[VictoryManager] Cliente tentou verificar vitória — ignorado.");
            return;
        }

        // Se qualquer porta ainda não tiver o jogador correto dentro, não vence
        foreach (var door in Instance.doors)
        {
            if (door == null)
            {
                Debug.LogWarning("[VictoryManager] Porta nula detectada, abortando verificação.");
                return;
            }

            if (!door.IsPlayerInside)
            {
                // Algum jogador ainda não chegou
                Debug.Log($"[VictoryManager] {door.AssignedPlayer?.name ?? "??"} ainda não chegou.");
                return;
            }
        }

        Debug.Log("[VictoryManager] ✅ Todos os jogadores chegaram à porta final! Enviando RPC de vitória global.");
        Instance.NotifyVictoryClientRpc();
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc()
    {
        Debug.Log("[VictoryManager] RPC de vitória recebido — exibindo painel de vitória.");

        if (GameManager.Instance != null)
            GameManager.Instance.ShowVictoryClientRpc();
        else
            Debug.LogWarning("[VictoryManager] GameManager.Instance é nulo — vitória não exibida!");
    }
}