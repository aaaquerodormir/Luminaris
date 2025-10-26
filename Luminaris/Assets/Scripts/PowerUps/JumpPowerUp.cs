using UnityEngine;

[CreateAssetMenu(menuName = "PowerUps/Jump Power Up")]
public class JumpPowerUp : PowerUpModificador
{
    [Header("Config do PowerUp")]
    [SerializeField] private int extraJumps = 1;

    public override void Activate(GameObject target)
    {
        if (target == null) return;
        var player = target.GetComponent<PlayerMovement>();
        if (player == null) return;

        // Chama o método do Player que adiciona o buff (servidor)
        player.AddJumpPowerup_Server(extraJumps, durationTurns);
        Debug.Log($"[JumpPowerUp] Aplicado em {player.name}: +{extraJumps} por {durationTurns} turns");
    }
}
