using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.3f; // velocidade base
    [SerializeField] private float growthPerTurn = 0.01f; // crescimento por turno
    [SerializeField] private float maxSpeed = 2f; // velocidade máxima

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    private float speedModifier = 1f;
    private int turnsLeft = 0;
    private int totalTurns = 0;
    private int savedTurns = 0;

    private float safeZoneHeight = -Mathf.Infinity;
    private float currentSpeed;
    private float lastSpeed;

    public struct LavaTurnInfo
    {
        public float currentSpeed;
        public float delta;
        public int totalTurns;

        public LavaTurnInfo(float currentSpeed, float delta, int totalTurns)
        {
            this.currentSpeed = currentSpeed;
            this.delta = delta;
            this.totalTurns = totalTurns;
        }
    }

    private void Awake()
    {
        currentSpeed = baseSpeed;
        lastSpeed = currentSpeed;
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        float dynamicSpeed = currentSpeed * speedModifier;
        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;
    }

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
    }

    public void ResetLava(Checkpoint checkpoint)
    {
        safeZoneHeight = checkpoint.LavaSafeHeight;
        transform.position = new Vector3(transform.position.x, safeZoneHeight, transform.position.z);

        speedModifier = 1f;
        turnsLeft = 0;
        totalTurns = savedTurns;

        currentSpeed = baseSpeed;
        lastSpeed = currentSpeed;
    }

    public void ResetLavaState()
    {
        safeZoneHeight = -Mathf.Infinity;
        speedModifier = 1f;
        turnsLeft = 0;
        totalTurns = 0;
        savedTurns = 0;

        currentSpeed = baseSpeed;
        lastSpeed = currentSpeed;
    }

    public void AddSpeedModifier(float modifier, int durationTurns)
    {
        speedModifier = modifier;
        turnsLeft = durationTurns;
    }

    public LavaTurnInfo ConsumeTurn()
    {
        totalTurns++;

        float oldSpeed = currentSpeed;
        float newSpeed = currentSpeed + growthPerTurn;
        currentSpeed = Mathf.Min(newSpeed, maxSpeed);

        float delta = currentSpeed - oldSpeed;

        if (turnsLeft > 0)
        {
            turnsLeft--;

            if (turnsLeft <= 0)
            {
                speedModifier = 1f;
            }
        }

        return new LavaTurnInfo(currentSpeed * speedModifier, delta, totalTurns);
    }

    public int GetSavedTurns() => savedTurns;

    public void LoadSavedTurns(int turns)
    {
        totalTurns = turns;
        savedTurns = turns;

        currentSpeed = baseSpeed + (growthPerTurn * totalTurns);
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        lastSpeed = currentSpeed;
    }

    public void SaveProgressAtCheckpoint()
    {
        savedTurns = totalTurns;
    }

    public void ResetSpeedAtCheckpoint()
    {
        currentSpeed = baseSpeed;
        lastSpeed = currentSpeed;
    }
}
