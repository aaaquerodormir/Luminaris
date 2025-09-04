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

    public bool TryActivate()
    {
        if (activated) return false; // já foi ativado
        activated = true;
        return true; // primeira vez
    }

    // usado pelo GameLoader para marcar checkpoints já alcançados
    public void PreActivate()
    {
        activated = true;
    }

    private void OnDrawGizmos()
    {
        if (respawnPoint == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(respawnPoint.position, 0.3f);

        Vector3 lavaPos = new Vector3(respawnPoint.position.x, LavaSafeHeight, respawnPoint.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lavaPos + Vector3.left * 10f, lavaPos + Vector3.right * 10f);
        Gizmos.DrawSphere(lavaPos, 0.2f);
    }
}
