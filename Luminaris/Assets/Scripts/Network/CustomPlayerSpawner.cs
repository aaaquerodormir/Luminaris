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

    // --- MUDANÇA 1: A "TRAVA" ---
    // Esta variável vai impedir que o spawn rode mais de uma vez.
    private bool playersSpawned = false;

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
            if (clientsCompleted.Count == totalClients)
            {
                // --- MUDANÇA 2: CHECAGEM DA TRAVA ---
                // Se os jogadores já foram spawnados, não faz mais nada.
                if (playersSpawned) return;

                // Ativa a trava para garantir que isso só rode uma vez.
                playersSpawned = true;
                // ------------------------------------

                Debug.Log($"[Spawner] Todos os {totalClients} clientes carregaram a cena. Iniciando spawn.");
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

        netObj.SpawnAsPlayerObject(clientId, true);
        Debug.Log($"[Spawner] Player {clientId} spawnado em {spawnPos}");

        // Registra o jogador no TurnControl (usando o script TurnControl que eu te passei)
        if (TurnControl.Instance != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
                TurnControl.Instance.RegisterPlayer(movement);
        }
    }

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
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        base.OnDestroy();
    }
}