using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Lava Speed Power Up")]
public class LavaSpeedPowerUp : PowerUpModificador
{
    [Header("Config do PowerUp")]
    [Tooltip("Multiplicador aplicado ao movimento do jogador (ex: 1.5 → +50% velocidade)")]
    [SerializeField] private float newMultiplier = 1.5f;

    public override void Activate(GameObject target)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        PlayerMovement player = target.GetComponent<PlayerMovement>();
        if (player == null) return;

        LavaRise lava = Object.FindAnyObjectByType<LavaRise>();
        if (lava != null)
        {
            lava.AddSpeedMultiplier(newMultiplier, DurationTurns, player.OwnerClientId);
        }
    }
}
