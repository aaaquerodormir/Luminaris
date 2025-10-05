using UnityEngine;
using Unity.Netcode;


public class CustomPlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject player1Prefab;
    [SerializeField] private GameObject player2Prefab;

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        GameObject prefab = clientId == NetworkManager.Singleton.LocalClientId
            ? player1Prefab
            : player2Prefab;

        Vector3 spawnPos = SpawnPointManager.Instance != null
            ? SpawnPointManager.Instance.GetSpawnPositionForClient(clientId)
            : Vector3.zero;

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        obj.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}
