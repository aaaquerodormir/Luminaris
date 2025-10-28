using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapPlatform : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField]
    private GameObject smokeEffectPrefab; // Prefab da fumaça

    [SerializeField]
    private GameObject enemyPrefab; // Prefab do Inimigo (DEVE ter um NetworkObject)

    [Header("Configurações")]
    [SerializeField]
    private Transform spawnPoint;

    private NetworkVariable<bool> isTriggered = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        // Garante que a plataforma seja um Trigger para o OnTriggetEnter2D
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered.Value) return;
        if (!IsServer) return;

        // Verifica se quem tocou é um jogador (use a Tag "Player")
        if (other.CompareTag("Player"))
        {
            isTriggered.Value = true;
            ulong triggeringPlayerId = other.GetComponent<NetworkObject>().OwnerClientId;
            Vector3 spawnPosition = (spawnPoint != null) ? spawnPoint.position : transform.position;

            SpawnTrapServerRpc(spawnPosition, triggeringPlayerId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnTrapServerRpc(Vector3 spawnPosition, ulong triggeringPlayerId)
    {
        // 1. Instancia o Inimigo
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        NetworkObject enemyNetworkObject = enemyInstance.GetComponent<NetworkObject>();
        enemyNetworkObject.Spawn(true);

        // 2. Informa ao inimigo qual jogador ele deve "pertencer"
        EnemyController enemyController = enemyInstance.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            // Esta chamada não muda
            enemyController.SetLinkedPlayerServerRpc(triggeringPlayerId);
        }

        // 3. Mostra a fumaça em todos os clientes
        SpawnSmokeClientRpc(spawnPosition);
    }

    [ClientRpc]
    private void SpawnSmokeClientRpc(Vector3 spawnPosition)
    {
        if (smokeEffectPrefab != null)
        {
            GameObject smoke = Instantiate(smokeEffectPrefab, spawnPosition, Quaternion.identity);
            Destroy(smoke, 2f); // Destrói a fumaça após 2 segundos
        }
    }
}