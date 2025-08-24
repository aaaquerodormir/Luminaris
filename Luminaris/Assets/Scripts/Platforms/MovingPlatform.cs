using UnityEngine;

public class MovingPlatform : MonoBehaviour, IResettable
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed;

    private Vector3 targetPos;
    private Vector3 startPos;

    private void Start()
    {
        startPos = transform.position;
        targetPos = pointB.position;

        // Registra no GameManager
        GameManager.Instance.RegisterResettable(this);
    }

    private void Update()
    {
        if (Vector2.Distance(transform.position, pointA.position) < 0.05f)
        {
            targetPos = pointB.position;
        }

        if (Vector2.Distance(transform.position, pointB.position) < 0.05f)
        {
            targetPos = pointA.position;
        }

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.parent = this.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            collision.transform.parent = null;
        }
    }

    // Reset da plataforma
    public void ResetState()
    {
        transform.position = startPos;
        targetPos = pointB.position;
    }
}
