using UnityEngine;

[CreateAssetMenu(menuName = "PowerUp/Pulo Simples")]
public class JumpPowerUp : ScriptableObject
{
    [SerializeField] private int extraJumps = 1;
    [SerializeField] private int durationTurns = 1;

    public void Apply(GameObject target)
    {
        var player = target.GetComponent<PlayerMovement>();
        if (player != null)
            player.AddJumpPowerUp(extraJumps, durationTurns);
    }
}
