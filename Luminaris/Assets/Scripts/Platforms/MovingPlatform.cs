using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class MovingPlatform : NetworkBehaviour, IResettable
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float pauseAtA = 1f;
    [SerializeField] private float pauseAtB = 1f;

    private Vector3 targetPos;
    private Vector3 startPos;

    private Vector3 previousPos;
    private Vector2 _calculatedVelocity;

    private bool isMoving = true;
    private float waitTimer = 0f;

    public Vector2 currentVelocity => _calculatedVelocity;

    private void Start()
    {
        if (pointA == null || pointB == null)
        {
            enabled = false;
            return;
        }

        startPos = transform.position;
        targetPos = pointB.position;
        previousPos = transform.position;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            if (isMoving)
            {
                MovePlatformLogic();
            }
            else
            {
                HandleWaitLogic();
            }
        }
        CalculateVelocity();
    }

    private void MovePlatformLogic()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            isMoving = false;

            if (targetPos == pointA.position)
            {
                waitTimer = pauseAtA;
                targetPos = pointB.position;
            }
            else
            {
                waitTimer = pauseAtB;
                targetPos = pointA.position;
            }
        }
    }

    private void HandleWaitLogic()
    {
        waitTimer -= Time.fixedDeltaTime;
        if (waitTimer <= 0)
        {
            isMoving = true;
        }
    }

    private void CalculateVelocity()
    {
        _calculatedVelocity = (transform.position - previousPos) / Time.fixedDeltaTime;
        previousPos = transform.position;
    }

    public void ResetState()
    {
        isMoving = true;
        waitTimer = 0f;

        if (IsServer)
        {
            transform.position = startPos;
        }

        targetPos = pointB.position;
        previousPos = transform.position;
        _calculatedVelocity = Vector2.zero;
    }

    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}