using UnityEngine;
using Unity.Netcode;

public class LavaRise : NetworkBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.3f;
    [SerializeField] private float growthPerTurn = 0.01f;
    [SerializeField] private float maxSpeed = 2f;

    private float speedModifier = 1f;

    // Variáveis de Turno
    private int turnsLeft = 0; // Turnos restantes do PowerUp de velocidade
    private int totalTurns = 0; // Turnos totais da partida (para o crescimento natural)

    private float server_totalBonusApplied = 0f;

    private float safeZoneHeight = -Mathf.Infinity;

    [Header("Debug Info")]
    [SerializeField] private float currentSpeed;

    // Variável para controlar o spam do Debug.Log
    private float debugTimer = 0f;

    private void Awake()
    {
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Cálculo da velocidade: Base + (Crescimento por Turno) * Modificador de PowerUp
        currentSpeed = (baseSpeed + (totalTurns * growthPerTurn)) * speedModifier;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        // Movimento
        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        // Limite de altura (opcional)
        if (transform.position.y > 50f)
            currentSpeed = 0;

        // Debug Log a cada 1 segundo
        debugTimer += Time.deltaTime;
        if (debugTimer >= 1.0f)
        {
            Debug.Log($"[LavaRise] Vel: {currentSpeed:F3} | Turnos: {totalTurns} | Mod: {speedModifier}");
            debugTimer = 0f;
        }
    }

    public void AddSpeedModifier(float newMultiplier, int duration)
    {
        if (!IsServer) return;

        float bonus = newMultiplier - 1.0f;
        if (bonus <= 0f)
        {
            Debug.LogWarning("[LavaRise-SERVER] Recebeu power-up de lava sem bônus.");
            return;
        }

        speedModifier += bonus;
        server_totalBonusApplied += bonus;
        turnsLeft = Mathf.Max(turnsLeft, duration);

        Debug.Log($"[LavaRise-SERVER] Modificador ACUMULADO. Novo mod: {speedModifier}. Duração: {turnsLeft} turnos.");
    }

    // --- CORREÇÃO AQUI ---
    // Este método é chamado pelo TurnControl toda vez que o turno acaba
    public void DecrementBuffTurns()
    {
        if (!IsServer) return;

        // 1. Aumenta o contador global de turnos (Faz a lava acelerar naturalmente)
        totalTurns++;

        // 2. Gerencia o tempo do Power-Up (se houver um ativo)
        if (turnsLeft > 0)
        {
            turnsLeft--;
            Debug.Log($"[LavaRise] Turnos de buff restantes: {turnsLeft}");

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

        Debug.Log($"[LavaRise-SERVER] Modificador expirou. Resetado para {speedModifier}.");
    }

    public void SaveProgressAtCheckpoint() => Debug.Log("[LavaRise] Progresso salvo.");

    public void ResetLava(Checkpoint cp)
    {
        transform.position = new Vector3(transform.position.x, cp.LavaSafeHeight, transform.position.z);
        // Opcional: Você pode querer resetar o totalTurns aqui também se quiser que a velocidade volte ao inicio
        totalTurns = 0; 
        currentSpeed = baseSpeed;
        Debug.Log("[LavaRise] Resetada no checkpoint.");
    }

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
        Debug.Log($"[LavaRise] SafeZone = {height}");
    }
}