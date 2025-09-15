using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Lava Speed Power Up")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [SerializeField] private float newMultiplier = 0.5f;

    public override void Activate(GameObject target)
    {
        var lava = GameObject.FindWithTag("Lava")?.GetComponent<LavaRise>();
        if (lava != null)
        {
            lava.AddSpeedModifier(newMultiplier, durationTurns);
            Debug.Log($"[PowerUp] Lava acelerada! Multiplicador = {newMultiplier} por {durationTurns} turnos");
        }
    }

    public override void Deactivate(GameObject target)
    {
        // A Lava já reseta quando os turnos acabam
    }
}
