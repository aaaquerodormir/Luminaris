using UnityEngine;

public class LavaRise : MonoBehaviour
{
    [Header("Velocidade da Lava")]
    [SerializeField] private float baseSpeed = 0.5f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Jogadores")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    private float speedModifier = 1f;
    private int turnsLeft = 0;

    private float safeZoneHeight = -Mathf.Infinity;
    private bool inSafeZone = false;

    public void SetSafeZone(float height)
    {
        safeZoneHeight = height;
        transform.position = new Vector3(transform.position.x, height, transform.position.z);
        inSafeZone = true;
    }

    private void Update()
    {
        if (player1 == null || player2 == null) return;

        if (inSafeZone)
        {
            if (player1.position.y > safeZoneHeight && player2.position.y > safeZoneHeight)
                inSafeZone = false;
            return;
        }

        float dynamicSpeed = baseSpeed * speedMultiplier * speedModifier;
        transform.position += Vector3.up * dynamicSpeed * Time.deltaTime;
    }

    public void ResetLava()
    {
        safeZoneHeight = -Mathf.Infinity;
        inSafeZone = false;
        speedModifier = 1f;
        turnsLeft = 0;
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
