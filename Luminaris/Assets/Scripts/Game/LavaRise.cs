using UnityEngine;
using Unity.Netcode;

public class LavaRise : NetworkBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.3f;
    [SerializeField] private float growthPerTurn = 0.01f;
    [SerializeField] private float maxSpeed = 2f;

    //[Header("Jogadores")]
    //[SerializeField] private Transform player1;
    //[SerializeField] private Transform player2;

    private float speedModifier = 1f;
    private int turnsLeft = 0;
    private int totalTurns = 0;
    //private int savedTurns = 0;

    // ⬇️ NOVO: Variável de servidor para rastrear o bônus total aplicado ⬇️
    private float server_totalBonusApplied = 0f;

    private float safeZoneHeight = -Mathf.Infinity;
    private float currentSpeed;
    //private float lastSpeed;


    private AudioSource lavaAudio;

    private void Awake()
    {
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        if (!IsServer) return;

        // ⬇️ MODIFICADO: Usa o 'currentSpeed' calculado ⬇️
        // (O seu código original já fazia isso, só garantindo)
        currentSpeed = (baseSpeed + (totalTurns * growthPerTurn)) * speedModifier;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        if (transform.position.y > 50f)
            currentSpeed = 0;
    }

    // ⬇️ NOVO: Função que o Power-Up chama (como da resposta anterior) ⬇️
    public void AddSpeedModifier(float newMultiplier, int duration)
    {
        if (!IsServer) return;

        // 1. Calcula o "bônus" (ex: 1.5x -> bônus de 0.5f)
        float bonus = newMultiplier - 1.0f;
        if (bonus <= 0f)
        {
            Debug.LogWarning("[LavaRise-SERVER] Recebeu power-up de lava sem bônus.");
            return;
        }

        // 2. SOMA o bônus ao multiplicador atual
        speedModifier += bonus;

        // 3. SOMA o bônus ao rastreador de servidor
        server_totalBonusApplied += bonus;

        // 4. Define a duração para o MÁXIMO
        turnsLeft = Mathf.Max(turnsLeft, duration);

        Debug.Log($"[LavaRise-SERVER] Modificador de velocidade ACUMULADO. Novo mod: {speedModifier}. Duração: {turnsLeft} turnos.");
    }

    // ⬇️ NOVO: Método chamado pelo TurnControl (sem alteração) ⬇️
    public void DecrementBuffTurns()
    {
        if (!IsServer || turnsLeft == 0) return;

        turnsLeft--;

        if (turnsLeft <= 0)
        {
            RevertSpeedModifier();
        }
    }

    // ⬇️ ALTERADO: Método privado que reverte o efeito no servidor ⬇️
    private void RevertSpeedModifier()
    {
        if (!IsServer) return;

        // 1. Subtrai o bônus TOTAL que acumulamos
        speedModifier -= server_totalBonusApplied;

        // 2. Reseta os rastreadores
        server_totalBonusApplied = 0f;
        turnsLeft = 0;

        // 3. Garante que a velocidade não fique abaixo do normal (segurança)
        if (speedModifier < 1.0f)
            speedModifier = 1.0f;

        Debug.Log($"[LavaRise-SERVER] Modificador de velocidade da lava expirou. Resetado para {speedModifier}.");
    }

    public void SaveProgressAtCheckpoint() => Debug.Log("[LavaRise] Progresso salvo.");
    public void ResetLava(Checkpoint cp)
    {
        transform.position = new Vector3(transform.position.x, cp.LavaSafeHeight, transform.position.z);
        currentSpeed = baseSpeed;
        Debug.Log("[LavaRise] Resetada no checkpoint.");
    }
    //public void AddSpeedModifier(float newMultiplier, int duration)
    //{
    //    // Guarda de segurança
    //    if (!IsServer) return;

    //    speedModifier = newMultiplier;
    //    turnsLeft = duration;

    //    Debug.Log($"[LavaRise-SERVER] Modificador de velocidade {newMultiplier} aplicado por {duration} turnos.");

    //    // TODO: Assim como no Player, o 'TurnControl'
    //    // precisa notificar a lava quando um turno passa
    //    // para decrementar 'turnsLeft' e resetar 'speedModifier' para 1.0f.
    //}

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
        Debug.Log($"[LavaRise] SafeZone = {height}");
    }
}

