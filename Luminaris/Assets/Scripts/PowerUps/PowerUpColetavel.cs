using UnityEngine;

public class PowerUpColetavel : MonoBehaviour
{
    [SerializeField] private JumpPowerUp powerUp;

    private void OnTriggerEnter2D(Collider2D col)
    {
        var player = col.GetComponent<PlayerMovement>();
        if (player != null)
        {
            powerUp.Apply(player.gameObject);
            Debug.Log("Power-up de pulo coletado!");
            Destroy(gameObject);
        }
    }
}
