using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;

public class CustomPlayerSpawner : NetworkBehaviour
{
    [Header("Player Prefabs")]
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    [Header("Câmeras Persistentes")]
    [Tooltip("O componente que controla o alvo da câmera do Player 1.")]
    [SerializeField] private Unity.Cinemachine.CinemachineCamera player1VirtualCam;
    [Tooltip("O componente que controla o alvo da câmera do Player 2.")]
    [SerializeField] private Unity.Cinemachine.CinemachineCamera player2VirtualCam;

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
            var player1 = SpawnPlayer(p1ClientId, player1Prefab, spawnP1);
            if (doorP1 != null)
                doorP1.SetTargetClientId(p1ClientId);

            // Atribui o jogador 1 à câmera 1
            if (player1 != null && player1VirtualCam != null)
            {
                player1VirtualCam.Follow = player1.transform;
                Debug.Log("[Spawner] Player 1 atribuído à Câmera Virtual 1.");
            }
        }

        if (clientIds.Length >= 2)
        {
            ulong p2ClientId = clientIds[1];
            var player2 = SpawnPlayer(p2ClientId, player2Prefab, spawnP2);
            if (doorP2 != null)
                doorP2.SetTargetClientId(p2ClientId);

            // Atribui o jogador 2 à câmera 2
            if (player2 != null && player2VirtualCam != null)
            {
                player2VirtualCam.Follow = player2.transform;
                Debug.Log("[Spawner] Player 2 atribuído à Câmera Virtual 2.");
            }
        }

        // A lógica de desativar a Fallback Camera foi removida, pois as câmeras virtuais ativas a substituirão.
    }

    private GameObject SpawnPlayer(ulong clientId, GameObject prefab, Transform spawnPoint)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawner] Prefab de jogador não atribuído!");
            return null;
        }

        var spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var spawnRot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        var player = Instantiate(prefab, spawnPos, spawnRot);
        var netObj = player.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError("[Spawner] Prefab não tem NetworkObject!");
            Destroy(player);
            return null;
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

        return player; // Retorna o objeto do jogador para que possa ser atribuído à câmera
    }

    // A lógica de desativar a Fallback Camera foi removida, pois as câmeras virtuais ativas a substituirão.
    // O GameManager ainda tem a lógica de reativar a Fallback Camera no Game Over.

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        base.OnDestroy();
    }
}