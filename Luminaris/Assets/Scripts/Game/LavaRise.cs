using UnityEngine;
using Unity.Netcode;

public class LavaRise : NetworkBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.3f;
    [SerializeField] private float growthPerTurn = 0.01f;
    [SerializeField] private float maxSpeed = 2f;

    private float speedModifier = 1f;
    private int turnsLeft = 0;
    private int totalTurns = 0; 

    private float server_totalBonusApplied = 0f;

    private float safeZoneHeight = -Mathf.Infinity;

    [Header("Debug Info")]
    [SerializeField] private float currentSpeed;

    private float debugTimer = 0f;

    private void Awake()
    {
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        if (!IsServer) return;

        currentSpeed = (baseSpeed + (totalTurns * growthPerTurn)) * speedModifier;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        if (transform.position.y > 50f)
            currentSpeed = 0;

    }

    public void AddSpeedModifier(float newMultiplier, int duration)
    {
        if (!IsServer) return;

        float bonus = newMultiplier - 1.0f;
        if (bonus <= 0f)
        {
            return;
        }

        speedModifier += bonus;
        server_totalBonusApplied += bonus;
        turnsLeft = Mathf.Max(turnsLeft, duration);
    }

    public void DecrementBuffTurns()
    {
        if (!IsServer) return;

        totalTurns++;

        if (turnsLeft > 0)
        {
            turnsLeft--;

            if (turnsLeft <= 0)
            {
                RevertSpeedModifier();
            }
        }
    }

    private void RevertSpeedModifier()
    {
        if (!IsServer) return;

        speedModifier -= server_totalBonusApplied;
        server_totalBonusApplied = 0f;
        turnsLeft = 0;

        if (speedModifier < 1.0f)
            speedModifier = 1.0f;
    }

    public void ResetLava(Checkpoint cp)
    {
        transform.position = new Vector3(transform.position.x, cp.LavaSafeHeight, transform.position.z);
        totalTurns = 0; 
        currentSpeed = baseSpeed;
    }

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
    }
}