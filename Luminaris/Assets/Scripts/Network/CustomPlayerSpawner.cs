using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class CustomPlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnP1;
    [SerializeField] private Transform spawnP2;

    // 🔹 Evento global para que HUDs saibam quando um jogador foi spawnado
    public static event System.Action<ulong, PlayerMovementUI> OnPlayerSpawned;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (sceneName == "SampleScene")
        {
            Debug.Log("[Spawner] Cena de jogo carregada, iniciando spawn dos jogadores...");
            StartCoroutine(SpawnPlayersWhenReady());
        }
    }

    private IEnumerator SpawnPlayersWhenReady()
    {
        yield return new WaitForSeconds(1f);

        // 🔹 Ordena IDs para garantir que o host (0) é sempre Player 1
        var clientIds = NetworkManager.Singleton.ConnectedClients.Keys.OrderBy(id => id).ToArray();
        Debug.Log($"[Spawner] Conectados: {clientIds.Length} jogadores (ordenados).");

        if (clientIds.Length >= 1)
            SpawnPlayer(clientIds[0], player1Prefab, spawnP1);

        if (clientIds.Length >= 2)
            SpawnPlayer(clientIds[1], player2Prefab, spawnP2);
    }

    private void SpawnPlayer(ulong clientId, GameObject prefab, Transform spawnPoint)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawner] Prefab de jogador não atribuído!");
            return;
        }

        var spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var spawnRot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        var player = Instantiate(prefab, spawnPos, spawnRot);
        var netObj = player.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[Spawner] Prefab não tem NetworkObject!");
            Destroy(player);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Spawner] Player {clientId} spawnado em {spawnPos}");

        // 🔹 Registra no TurnControl se já existir
        if (TurnControl.Instance != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
                TurnControl.Instance.RegisterPlayer(movement);
        }
        // 🔹 Notifica todos os HUDs sobre o novo jogador
        var ui = player.GetComponent<PlayerMovementUI>();
        if (ui != null)
        {
            Debug.Log($"[Spawner] PlayerMovementUI do Client {clientId} detectado e notificado.");
            OnPlayerSpawned?.Invoke(clientId, ui);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
    }
}
