using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.5f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    [SerializeField] private Vector3 startPos;

    private float speedModifier = 1f;
    private int turnsLeft = 0;

    private void Awake()
    {
        if (startPos == Vector3.zero)
            startPos = transform.position;
    }

    private void Start()
    {
        startPos = transform.position;
        transform.position = startPos;
    }

    public void ResetLava()
    {
        transform.position = startPos;
        speedModifier = 1f;
        turnsLeft = 0;
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        float dynamicSpeed = baseSpeed * speedMultiplier * speedModifier;
        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;
    }

    public void AddSpeedModifier(float modifier, int durationTurns)
    {
        speedModifier = modifier;
        turnsLeft = durationTurns;
        Debug.Log($"Lava modificada! Novo multiplicador: {speedModifier}, duração: {turnsLeft} turnos");
    }

    public void ConsumeTurn()
    {
        if (turnsLeft > 0)
        {
            turnsLeft--;
            if (turnsLeft <= 0)
            {
                speedModifier = 1f;
                Debug.Log("Lava voltou à velocidade normal");
            }
        }
    }
}
