using UnityEngine;

public class PowerUpColetavel : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D col)
    {
        ActivatePowerUp();
    }

    void ActivatePowerUp()
    {
        // Implementar efeito do power-up aqui
        Debug.Log("Power-up ativado!");
        // Destrói o power-up após ser coletado
        Destroy(gameObject);
    }
}
