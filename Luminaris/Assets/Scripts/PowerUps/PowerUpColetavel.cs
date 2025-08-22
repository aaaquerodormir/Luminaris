using UnityEngine;

public class PowerUpColetavel : MonoBehaviour
{
    [SerializeField] private PowerUpModificador powerModificador;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (powerModificador == null) return;

        // Se for PowerUp de pulo → aplica no jogador que encostou
        if (powerModificador is JumpPowerUp)
        {
            powerModificador.Activate(col.gameObject);
        }
        else
        {
            // Caso contrário (ex: Lava) → ativa sem precisar do jogador
            powerModificador.Activate(null);
        }

        Destroy(gameObject);
    }
}
