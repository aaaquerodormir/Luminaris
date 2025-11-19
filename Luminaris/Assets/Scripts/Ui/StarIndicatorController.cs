using UnityEngine;

public class StarIndicatorController : MonoBehaviour
{
    public static StarIndicatorController Instance { get; private set; }

    [SerializeField] private Vector2 offset = new Vector2(-50, 0);
    private RectTransform starRect;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            starRect = GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Hide();
    }
    public void ShowAndFollow(RectTransform targetButton)
    {
        if (targetButton == null)
        {
            Hide();
            return;
        }

        // Define o pai da estrela como o pai do botão para garantir o mesmo espaço de coordenadas
        starRect.SetParent(targetButton.parent);

        // Calcula a posição
        starRect.anchoredPosition = targetButton.anchoredPosition + offset;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
