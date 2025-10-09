using UnityEngine;
using Unity.Netcode;

public class PowerUpColetavel : MonoBehaviour, IResettable
{
    [SerializeField] private GameObject feedBackTextualPrefab;
    [SerializeField] private PowerUpModificador powerModificador;

    [Header("Mensagem do Feedback")]
    [SerializeField] private string mensagemFeedback = "";

    private Vector3 startPos;
    private bool collected = false;

    private void Start()
    {
        startPos = transform.position;
        GameManager.Instance.RegisterResettable(this);
        Debug.Log($"[PowerUpColetavel] Registrado resetável {gameObject.name}");
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player") && powerModificador != null && !collected)
        {
            Debug.Log($"[PowerUpColetavel] {col.name} coletou {gameObject.name}");
            powerModificador.Activate(col.gameObject);
            collected = true;

            AudioManager.Instance.PlaySound("PowerUp");
            ShowFeedback(mensagemFeedback, transform.position + Vector3.up * 1f);
            gameObject.SetActive(false);
        }
    }

    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;
        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);
        var textComp = temp.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }

    public void ResetState()
    {
        collected = false;
        transform.position = startPos;
        gameObject.SetActive(true);
        Debug.Log($"[PowerUpColetavel] Resetado {gameObject.name}");
    }
}