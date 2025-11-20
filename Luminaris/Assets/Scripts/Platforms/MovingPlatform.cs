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

    // Variável para calcular a velocidade baseada no movimento REAL do objeto nesta máquina
    private Vector3 previousPos;
    private Vector2 _calculatedVelocity;

    private bool isMoving = true;
    private float waitTimer = 0f;

    // Propriedade pública acessada pelo PlayerMovement
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
        // IMPORTANTE: Não desabilitamos o script no cliente!
        // Precisamos dele rodando para calcular a velocidade, 
        // mas bloquearemos a lógica de "Mover" dentro do FixedUpdate.
    }

    private void FixedUpdate()
    {
        // 1. Lógica de Movimento (APENAS SERVIDOR)
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

        // 2. Cálculo de Velocidade (SERVIDOR E CLIENTE)
        // Todos calculam a velocidade baseada em onde a plataforma estava e onde ela está agora.
        // No Cliente, isso vai pegar o movimento suave do NetworkTransform.
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
        // A mágica acontece aqui:
        // Calculamos a velocidade baseada na diferença de posição deste frame para o anterior.
        // Se o NetworkTransform interpolar o movimento, essa velocidade vai refletir isso perfeitamente.
        _calculatedVelocity = (transform.position - previousPos) / Time.fixedDeltaTime;
        previousPos = transform.position;
    }

    public void ResetState()
    {
        // Reset visual e lógico
        isMoving = true;
        waitTimer = 0f;

        // Se for Servidor, força a posição. 
        // O NetworkTransform vai propagar isso para os clientes.
        if (IsServer)
        {
            transform.position = startPos;
        }

        targetPos = pointB.position;
        previousPos = transform.position; // Reseta previousPos para não gerar velocidade gigante
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