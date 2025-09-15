using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.5f;
    [SerializeField] private float growthPerTurn = 0.05f;

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    private float speedModifier = 1f;
    private int turnsLeft = 0;
    private int totalTurns = 0;
    private int savedTurns = 0; // salvo no checkpoint

    private float safeZoneHeight = -Mathf.Infinity;

    // histórico para calcular delta por turno
    private float lastSpeed = 0f;

    // Struct retornado ao consumir um turno
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
        // inicializa lastSpeed com o valor atual
        lastSpeed = baseSpeed + (totalTurns * growthPerTurn);
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        // velocidade atual aplicada ao movimento (frame)
        float dynamicSpeed = (baseSpeed + (totalTurns * growthPerTurn)) * speedModifier;
        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;
    }

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
    }

    // Reseta posição da lava e aplica o progresso salvo no checkpoint
    public void ResetLava(Checkpoint checkpoint)
    {
        safeZoneHeight = checkpoint.LavaSafeHeight;
        transform.position = new Vector3(transform.position.x, safeZoneHeight, transform.position.z);

        speedModifier = 1f;
        turnsLeft = 0;
        totalTurns = savedTurns;

        // atualiza histórico
        lastSpeed = baseSpeed + (totalTurns * growthPerTurn);
    }

    public void ResetLavaState()
    {
        safeZoneHeight = -Mathf.Infinity;
        speedModifier = 1f;
        turnsLeft = 0;
        totalTurns = 0;
        savedTurns = 0;
        lastSpeed = baseSpeed;
    }

    // Aplica powerup (multiplicador por N turnos)
    public void AddSpeedModifier(float modifier, int durationTurns)
    {
        speedModifier = modifier;
        turnsLeft = durationTurns;
        Debug.Log($"[Lava] PowerUp aplicado: multiplicador = {speedModifier}, duração = {turnsLeft} turnos");
    }

    // Consome um turno: incrementa contador, calcula velocidade atual e delta,
    // reduz duração do powerup e retorna informações para logging.
    public LavaTurnInfo ConsumeTurn()
    {
        totalTurns++;

        float currentSpeed = (baseSpeed + (totalTurns * growthPerTurn)) * speedModifier;
        float delta = currentSpeed - lastSpeed;
        lastSpeed = currentSpeed;

        Debug.Log($"[Lava] Turno {totalTurns} | Velocidade atual = {currentSpeed:F3} | Aumento neste turno = {delta:F3}");

        if (turnsLeft > 0)
        {
            turnsLeft--;
            Debug.Log($"[Lava] PowerUp ativo. Restam {turnsLeft} turnos (multiplicador {speedModifier})");

            if (turnsLeft <= 0)
            {
                speedModifier = 1f;
                Debug.Log("[Lava] PowerUp terminou. Velocidade voltou ao normal.");
            }
        }

        return new LavaTurnInfo(currentSpeed, delta, totalTurns);
    }

    public int GetSavedTurns() => savedTurns;

    // Carrega progresso salvo ao iniciar fase/continuar
    public void LoadSavedTurns(int turns)
    {
        totalTurns = turns;
        savedTurns = turns;
        lastSpeed = baseSpeed + (totalTurns * growthPerTurn);
        Debug.Log($"[Lava] Carregada com {savedTurns} turnos acumulados");
    }

    // Salva progresso atual no checkpoint
    public void SaveProgressAtCheckpoint()
    {
        savedTurns = totalTurns;
        Debug.Log($"[Lava] Progresso salvo: {savedTurns} turnos acumulados");
    }
}
