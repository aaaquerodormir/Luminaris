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

    private bool canMove = true;

    // A velocidade que o PlayerMovement usará
    public Vector2 currentVelocity { get; private set; }

    private void Start()
    {
        startPos = transform.position;
        targetPos = pointB.position;
        previousPos = transform.position;

        //GameManager.Instance.RegisterResettable(this);

        StartCoroutine(ControlMovementCycle());
    }

    private void FixedUpdate()
    {
        // 1. Lógica de Movimento (Usando Time.fixedDeltaTime)
        if (canMove)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.fixedDeltaTime);
        }

        // 2. Cálculo da Velocidade
        currentVelocity = (transform.position - previousPos) / Time.fixedDeltaTime;
        previousPos = transform.position;
    }

    private IEnumerator ControlMovementCycle()
    {
        while (true)
        {
            // Espera até que a plataforma chegue ao destino
            yield return new WaitUntil(() => Vector3.Distance(transform.position, targetPos) < 0.05f);

            // 1. Para o movimento
            canMove = false;

            // 2. Pausa
            if (targetPos == pointA.position)
                yield return new WaitForSeconds(pauseAtA);
            else
                yield return new WaitForSeconds(pauseAtB);

            // 3. Define o novo destino e reinicia o movimento
            targetPos = targetPos == pointA.position ? pointB.position : pointA.position;
            canMove = true;

            yield return null;
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();
        transform.position = startPos;
        targetPos = pointB.position;
        previousPos = startPos;
        canMove = true;
        StartCoroutine(ControlMovementCycle());
    }
}
