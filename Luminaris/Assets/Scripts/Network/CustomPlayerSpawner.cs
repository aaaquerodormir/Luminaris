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

    private bool playersSpawned = false;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
    }

    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (sceneName == "SampleScene" || sceneName == "Fase2" || sceneName == "FaseFinal")
        {
            int totalClients = NetworkManager.Singleton.ConnectedClients.Count;

            if (clientsCompleted.Count == totalClients)
            {
                if (playersSpawned) return;

                playersSpawned = true;

                SpawnPlayersNow();
            }
        }
    }

    private void SpawnPlayersNow()
    {
        var clientIds = NetworkManager.Singleton.ConnectedClients.Keys.OrderBy(id => id).ToArray();
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
            }
        }
    }

    private GameObject SpawnPlayer(ulong clientId, GameObject prefab, Transform spawnPoint)
    {
        if (prefab == null)
        {
            return null;
        }

        var spawnPos = spawnPoint ? spawnPoint.position : Vector3.zero;
        var spawnRot = spawnPoint ? spawnPoint.rotation : Quaternion.identity;

        var player = Instantiate(prefab, spawnPos, spawnRot);
        var netObj = player.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Destroy(player);
            return null;
        }

        netObj.SpawnAsPlayerObject(clientId, true);
        if (TurnControl.Instance != null)
        {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
                TurnControl.Instance.RegisterPlayer(movement);
        }

        return player;
    }


    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoaded;
        base.OnDestroy();
    }
}