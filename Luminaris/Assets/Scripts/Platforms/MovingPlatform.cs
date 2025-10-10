using UnityEngine;
using System.Collections;

public class MovingPlatform : MonoBehaviour, IResettable
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float pauseAtA = 0f;
    [SerializeField] private float pauseAtB = 0f;

    private Vector3 targetPos;
    private Vector3 startPos;
    private bool isMoving = true;

    private void Start()
    {
        startPos = transform.position;
        targetPos = pointB.position;

        //GameManager.Instance.RegisterResettable(this);

        StartCoroutine(MovePlatform());
    }

    private IEnumerator MovePlatform()
    {
        while (true)
        {
            if (isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (Vector2.Distance(transform.position, targetPos) < 0.05f)
                {
                    isMoving = false;

                    if (targetPos == pointA.position)
                        yield return new WaitForSeconds(pauseAtA);
                    else
                        yield return new WaitForSeconds(pauseAtB);

                    targetPos = targetPos == pointA.position ? pointB.position : pointA.position;
                    isMoving = true;
                }
            }

            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            collision.transform.parent = transform;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            collision.transform.parent = null;
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.position = startPos;
        targetPos = pointB.position;
        isMoving = true;
        StartCoroutine(MovePlatform());
    }
}