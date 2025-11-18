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
    private Vector3 previousPos;
    private bool isMoving = true;
    public Vector2 currentVelocity;

    private void Start()
    {
        startPos = transform.position;
        targetPos = pointB.position;
        previousPos = transform.position;

        //GameManager.Instance.RegisterResettable(this);

        StartCoroutine(MovePlatform());
    }

    private void FixedUpdate()
    {
        currentVelocity = (transform.position - previousPos) / Time.fixedDeltaTime;
        previousPos = transform.position;
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

    public void ResetState()
    {
        StopAllCoroutines();
        transform.position = startPos;
        targetPos = pointB.position;
        previousPos = startPos;
        isMoving = true;
        StartCoroutine(MovePlatform());
    }
}