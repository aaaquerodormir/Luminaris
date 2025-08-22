using UnityEngine;

public class PowerUpColetavel : MonoBehaviour
{
    [SerializeField] private PowerUpModificador powerModificador;
    [SerializeField] private GameObject target; // pode ser Player ou Lava

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (powerModificador == null || target == null) return;

        powerModificador.Activate(target);
        Debug.Log($" PowerUp {powerModificador.name} ativado em {target.name}");

        Destroy(gameObject);
    }
}
