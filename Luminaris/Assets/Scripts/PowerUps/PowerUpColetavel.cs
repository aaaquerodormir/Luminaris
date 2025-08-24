using UnityEngine;

public class PowerUpColetavel : MonoBehaviour, IResettable
{
    [SerializeField] private PowerUpModificador powerModificador;

    private Vector3 startPos;
    private bool collected = false;

    private void Start()
    {
        startPos = transform.position;

        GameManager.Instance.RegisterResettable(this);  // Registra no GameManager para ser resetado
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && powerModificador != null && !collected)
        {
            powerModificador.Activate(col.gameObject);
            collected = true;
            gameObject.SetActive(false);
        }
    }

    // Restaura o estado inicial
    public void ResetState()
    {
        collected = false;
        transform.position = startPos;
        gameObject.SetActive(true);
    }
}
