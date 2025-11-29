using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapPlatform : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject smokeEffectPrefab;

    [SerializeField]
    private GameObject enemyPrefab;

    [Header("Configurações")]
    [SerializeField]
    private Transform spawnPoint;

    private NetworkVariable<bool> isTriggered = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered.Value) return;
        if (!IsServer) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered.Value = true;
            ulong triggeringPlayerId = collision.GetComponent<NetworkObject>().OwnerClientId;
            Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;

            SpawnTrapServerRpc(spawnPosition, triggeringPlayerId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTrapServerRpc(Vector3 spawnPosition, ulong triggeringPlayerId)
    {
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject enemyNetworkObject = enemyInstance.GetComponent<NetworkObject>();
        enemyNetworkObject.Spawn(true);

        EnemyController enemyController = enemyInstance.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.SetLinkedPlayerServerRpc(triggeringPlayerId);
        }
        SpawnSmokeClientRpc(spawnPosition);
    }

    [ClientRpc]
    private void SpawnSmokeClientRpc(Vector3 spawnPosition)
    {
        if (smokeEffectPrefab != null)
        {
            GameObject smoke = Instantiate(smokeEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(smoke, 2f);
        }
    }
}