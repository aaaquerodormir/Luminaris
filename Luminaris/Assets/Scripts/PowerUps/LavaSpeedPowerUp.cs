using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Lava Speed Power Up")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [Header("Config do PowerUp")]
    [SerializeField] private float newMultiplier = 1.5f;

    public override void Activate(GameObject target)
    {
        var lava = Object.FindFirstObjectByType<LavaRise>();
        if (lava == null) return;

        lava.AddSpeedModifier(newMultiplier, durationTurns);
    }

    public override void Deactivate(GameObject target)
    {
    }
}

