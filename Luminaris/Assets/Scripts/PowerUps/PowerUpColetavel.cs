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
        // Destr�i o power-up ap�s ser coletado
        Destroy(gameObject);
    }
}
