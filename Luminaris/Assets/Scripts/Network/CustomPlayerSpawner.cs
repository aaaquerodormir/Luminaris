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

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }
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
        // Espera um pouquinho para garantir que tudo está inicializado
        yield return new WaitForSeconds(1f);

        var clientIds = NetworkManager.Singleton.ConnectedClients.Keys.ToArray();

        Debug.Log($"[Spawner] Conectados: {clientIds.Length} jogadores.");

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

        var spawnPos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        var spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

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
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        }
    }
}
