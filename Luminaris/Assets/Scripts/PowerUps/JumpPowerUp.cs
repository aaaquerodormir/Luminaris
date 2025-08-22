using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Pulo Simples")]
public class JumpPowerUp : PowerUpModificador
{
    [SerializeField] private int extraJumps = 1;

    public override void Activate(GameObject target)
    {
        var player = target.GetComponent<PlayerMovement>();
        if (player != null)
        {
            player.AddJumpPowerUp(extraJumps, durationTurns);
        }
    }

    public override void Deactivate(GameObject target)
    {
        // Como os turnos já expiram no PlayerMovement, não precisa remover manualmente
    }
}
