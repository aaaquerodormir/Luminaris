using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Lava Speed Power Up")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [SerializeField] private float newMultiplier = 0.5f;

    public override void Activate(GameObject target)
    {
        var lava = Object.FindFirstObjectByType<LavaRise>();
        if (lava == null) return;

        //lava.AddSpeedModifier(newMultiplier, durationTurns);
    }

    public override void Deactivate(GameObject target)
    {
        // Lava já reseta sozinha quando turnsLeft <= 0
    }
}
