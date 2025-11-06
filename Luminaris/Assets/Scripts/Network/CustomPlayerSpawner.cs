// CustomPlayerSpawner.cs (Integrado e Corrigido para Cliente)
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

    [Header("Portas Finais (Na Cena)")]
    [SerializeField] private FinalDoor doorP1; // Porta do P1
    [SerializeField] private FinalDoor doorP2; // Porta do P2
    // 🔹 Evento global para que HUDs saibam quando um jogador foi spawnado
    //public static event System.Action<ulong, PlayerMovementUI> OnPlayerSpawned;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        // O GameFlowManager cuida da transição, mas o spawner ainda precisa saber quando a cena de JOGO carregou
        if (sceneName == "SampleScene" || sceneName == "Fase2Final" || sceneName == "Fase3") // Ajuste os nomes das cenas
        {
            Debug.Log("[Spawner] Cena de jogo carregada, iniciando spawn dos jogadores...");
            StartCoroutine(SpawnPlayersWhenReady());
        }
    }

    private IEnumerator SpawnPlayersWhenReady()
    {
        // Seu tempo de espera original
        yield return new WaitForSeconds(1f);

        // Ordena IDs para garantir que o host (0) é sempre Player 1
        var clientIds = NetworkManager.Singleton.ConnectedClients.Keys.OrderBy(id => id).ToArray();
        Debug.Log($"[Spawner] Conectados: {clientIds.Length} jogadores (ordenados).");

        // Spawn Player 1 (ID 0)
        if (clientIds.Length >= 1)
        {
            ulong p1ClientId = clientIds[0];
            SpawnPlayer(p1ClientId, player1Prefab, spawnP1);

            // AÇÃO: Atribui o Client ID à porta do P1
            if (doorP1 != null)
                doorP1.SetTargetClientId(p1ClientId);
        }

        // Spawn Player 2 (ID 1)
        if (clientIds.Length >= 2)
        {
            ulong p2ClientId = clientIds[1];
            SpawnPlayer(p2ClientId, player2Prefab, spawnP2);

            // AÇÃO: Atribui o Client ID à porta do P2
            if (doorP2 != null)
                doorP2.SetTargetClientId(p2ClientId);
        }

        // CORREÇÃO: Chama o ClientRpc para desativar a câmera em TODOS os clientes
        DisableFallbackCameraClientRpc();
    }

    private void SpawnPlayer(ulong clientId, GameObject prefab, Transform spawnPoint)
    {
        // ... (Seu código de SpawnPlayer original) ...
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

        // Registra no TurnControl
        if (TurnControl.Instance != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
                TurnControl.Instance.RegisterPlayer(movement);
        }
    }

    // NOVO MÉTODO RPC: Desativa a câmera de fallback em todos os clientes
    [ClientRpc]
    private void DisableFallbackCameraClientRpc()
    {
        // Esta função é executada em TODOS os clientes (incluindo o Host/Servidor)

        // Encontra o objeto da câmera de fallback pela tag
        GameObject[] fallbackCameras = GameObject.FindGameObjectsWithTag("FallbackCamera");
        // Verifica se encontrou alguma câmera
        if (fallbackCameras.Length > 0)
        {
            // Itera (passa por) CADA câmera encontrada na lista
            foreach (GameObject cam in fallbackCameras)
            {
                // Desativa o objeto da câmera de fallback localmente
                cam.SetActive(false);
            }

            // Log atualizado para mostrar quantas câmeras foram desativadas
            Debug.Log($"[CustomPlayerSpawner] {fallbackCameras.Length} câmera(s) de fallback desativada(s) LOCALMENTE em todos os clientes.");
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
