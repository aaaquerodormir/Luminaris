using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class CustomPlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnP1;
    [SerializeField] private Transform spawnP2;

    [Header("Portas Finais (Na Cena)")]
    [SerializeField] private FinalDoor doorP1;
    [SerializeField] private FinalDoor doorP2;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        // Apenas o servidor pode instanciar jogadores.
        if (!IsServer) return;

        // Verifica se é uma cena de jogo válida
        if (sceneName == "SampleScene" || sceneName == "Fase2Final" || sceneName == "Fase3")
        {
            int totalClients = NetworkManager.Singleton.ConnectedClients.Count;

            // Verifica se TODOS os clientes terminaram de carregar a cena.
            // Isso garante que o spawn só ocorra quando todos estiverem prontos.
            if (clientsCompleted.Count == totalClients)
            {
                Debug.Log($"[Spawner] Todos os {totalClients} clientes carregaram a cena. Iniciando spawn imediato.");

                // Chama o método de spawn imediatamente.
                SpawnPlayersNow();
            }
        }
    }

    private void SpawnPlayersNow()
    {
        // Ordena IDs para garantir que o host (0) é sempre Player 1
        var clientIds = NetworkManager.Singleton.ConnectedClients.Keys.OrderBy(id => id).ToArray();
        Debug.Log($"[Spawner] Conectados: {clientIds.Length} jogadores (ordenados).");

        if (clientIds.Length >= 1)
        {
            ulong p1ClientId = clientIds[0];
            SpawnPlayer(p1ClientId, player1Prefab, spawnP1);
            if (doorP1 != null)
                doorP1.SetTargetClientId(p1ClientId);
        }

        if (clientIds.Length >= 2)
        {
            ulong p2ClientId = clientIds[1];
            SpawnPlayer(p2ClientId, player2Prefab, spawnP2);
            if (doorP2 != null)
                doorP2.SetTargetClientId(p2ClientId);
        }

        // Desativa as câmeras de fallback em todos os clientes
        DisableFallbackCameraClientRpc();
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

        // Instancia o objeto na rede e dá a propriedade (ownership) ao cliente específico
        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Spawner] Player {clientId} spawnado em {spawnPos}");

        // Registra o jogador em outros sistemas (ex: TurnControl)
        if (TurnControl.Instance != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
                TurnControl.Instance.RegisterPlayer(movement);
        }
    }

    // Desativa a(s) câmera(s) de fallback em todos os clientes
    [ClientRpc]
    private void DisableFallbackCameraClientRpc()
    {
        GameObject[] fallbackCameras = GameObject.FindGameObjectsWithTag("FallbackCamera");

        if (fallbackCameras.Length > 0)
        {
            foreach (GameObject cam in fallbackCameras)
            {
                cam.SetActive(false);
            }
            Debug.Log($"[CustomPlayerSpawner] {fallbackCameras.Length} câmera(s) de fallback desativada(s).");
        }
        else
        {
            Debug.LogWarning("[CustomPlayerSpawner] Nenhuma câmera de fallback foi encontrada (Tag: FallbackCamera).");
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        base.OnDestroy();
    }
}