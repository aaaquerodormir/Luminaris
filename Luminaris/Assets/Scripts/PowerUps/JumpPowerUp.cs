using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Jump Power Up")]
public class JumpPowerUp : PowerUpModificador
{
    [Header("Config do PowerUp")]
    [SerializeField] private int extraJumps = 1;

    public override void Activate(GameObject target)
    {
        PlayerMovement player = target.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.ApplyJumpPowerUp(extraJumps, durationTurns);
            Debug.Log($"PowerUp de Pulo ativado em {player.name} (+{extraJumps} pulos por {durationTurns} turnos)");
        }
    }

    public override void Deactivate(GameObject target)
    {
        // Como já consumimos por turnos no Player, nada a fazer aqui
    }
}
