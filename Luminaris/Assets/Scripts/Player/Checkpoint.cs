using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointIndex;
    [SerializeField] private int groupId;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float lavaOffset = 3f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    public int Index => checkpointIndex;
    public int GroupId => groupId;
    public Vector3 RespawnPosition => respawnPoint.position;
    public float LavaSafeHeight => respawnPoint.position.y - lavaOffset;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
        }

        if (animator != null)
        {
            animator.enabled = true;
        }
    }

    private void OnDrawGizmos()
    {
        if (respawnPoint == null) return;

        Vector3 lavaPos = new Vector3(respawnPoint.position.x, LavaSafeHeight, respawnPoint.position.z);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(lavaPos + Vector3.left * 10f, lavaPos + Vector3.right * 10f);
        Gizmos.DrawSphere(lavaPos, 0.2f);
    }
}