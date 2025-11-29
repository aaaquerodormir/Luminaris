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

        starRect.SetParent(targetButton.parent);

        starRect.anchoredPosition = targetButton.anchoredPosition + offset;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
