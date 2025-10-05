using UnityEngine;
using Unity.Netcode;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetSpawnPositionForClient(ulong clientId)
    {
        if (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId)
            return player1Spawn.position;
        return player2Spawn.position;
    }
}