using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Lava Speed")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [SerializeField] private float speedModifier = 0.5f; // ex: 0.5 = metade da velocidade

    public override void Activate(GameObject target)
    {
        var lava = target.GetComponent<LavaRise>();
        if (lava != null)
        {
            lava.AddSpeedModifier(speedModifier, durationTurns);
        }
    }

    public override void Deactivate(GameObject target)
    {
        // O LavaRise já reseta sozinho quando turnos acabam
    }
}
