using UnityEngine;

public class PowerUpColetavel : MonoBehaviour, IResettable
{
    [SerializeField] GameObject feedBackTextualPrefab;
    [SerializeField] private PowerUpModificador powerModificador;

    [Header("Mensagem do Feedback")]
    [SerializeField] private string mensagemFeedback = "PowerUp";

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

            // Mostra feedback acima do powerup
            ShowFeedback(mensagemFeedback, transform.position + Vector3.up * 1.25f);

            gameObject.SetActive(false);
        }
    }

    // Método auxiliar para feedback textual
    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);

        // Se o prefab tiver um componente de texto, troca a mensagem
        var textComp = temp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }

    // Restaura o estado inicial
    public void ResetState()
    {
        collected = false;
        transform.position = startPos;
        gameObject.SetActive(true);
    }
}
