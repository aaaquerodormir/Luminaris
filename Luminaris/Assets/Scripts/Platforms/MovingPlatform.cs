using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed;
    Vector3 targetPos;

    private void Start()
    {
        targetPos = pointB.position;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Vector2.Distance(transform.position, pointA.position) < 0.05f)
        {
            targetPos = pointB.position;
        }

        if (Vector2.Distance(transform.position, pointB.position)< 0.05f)
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
}
