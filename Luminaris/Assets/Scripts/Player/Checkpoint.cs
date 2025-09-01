using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float lavaOffset = 3f;

    public int Index => checkpointIndex;
    public Vector3 RespawnPosition => respawnPoint.position;
    public float LavaSafeHeight => respawnPoint.position.y - lavaOffset;

    private bool activated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activated) return;

        if (other.CompareTag("Player"))
        {
            activated = true;
            GameManager.Instance.ReachCheckpoint(transform);
            Debug.Log($"Checkpoint {checkpointIndex} ativado por {other.name}");
        }
    }

    private void OnDrawGizmos()
    {
        if (respawnPoint == null) return;

        // Respawn do jogador
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(respawnPoint.position, 0.3f);

        // Altura da lava (safezone)
        Vector3 lavaPos = new Vector3(respawnPoint.position.x, LavaSafeHeight, respawnPoint.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lavaPos + Vector3.left * 10f, lavaPos + Vector3.right * 10f);
        Gizmos.DrawSphere(lavaPos, 0.2f);
    }
}
