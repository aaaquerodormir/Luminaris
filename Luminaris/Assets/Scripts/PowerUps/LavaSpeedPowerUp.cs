using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Lava Speed Power Up")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [Header("Config do PowerUp")]
    [Tooltip("Multiplicador aplicado ao movimento do jogador (ex: 1.5 → +50% velocidade)")]
    [SerializeField] private float newMultiplier = 1.5f;

    public override void Activate(GameObject target)
    {
        var lava = Object.FindFirstObjectByType<LavaRise>();
        if (lava == null) return;

        lava.AddSpeedModifier(newMultiplier, durationTurns);
    }

    public override void Deactivate(GameObject target)
    {
        // Lava já reseta sozinha quando turnsLeft <= 0
    }
}

