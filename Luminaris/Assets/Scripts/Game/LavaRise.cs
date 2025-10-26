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

    //private float speedModifier = 1f;
    //private int turnsLeft = 0;
    //private int totalTurns = 0;
    //private int savedTurns = 0;

    //private float safeZoneHeight = -Mathf.Infinity;
    //private float currentSpeed;
    //private float lastSpeed;

    private float currentY;

    private AudioSource lavaAudio;

    private void Awake()
    {
        currentSpeed = baseSpeed;
    }

    private void Update()
    {
        if (!IsServer) return;

        transform.position += Vector3.up * currentSpeed * Time.deltaTime;

        // trava a altura para não passar do teto
        if (transform.position.y > 50f)
            currentSpeed = 0;
    }

    public void SaveProgressAtCheckpoint() => Debug.Log("[LavaRise] Progresso salvo.");
    public void ResetLava(Checkpoint cp)
    {
        transform.position = new Vector3(transform.position.x, cp.LavaSafeHeight, transform.position.z);
        currentSpeed = baseSpeed;
        Debug.Log("[LavaRise] Resetada no checkpoint.");
    }

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
        Debug.Log($"[LavaRise] SafeZone = {height}");
    }
}
