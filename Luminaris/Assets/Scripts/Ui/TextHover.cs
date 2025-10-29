using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                         IPointerClickHandler, ISelectHandler
{
    [Header("Referências")]
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject normalBackgroundGO;   // GameObject da imagem de fundo padrão
    [SerializeField] private GameObject hoverBackgroundGO;    // GameObject da imagem de fundo em hover

    [Header("Cores do Texto")]
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color highlightedColor = Color.gray;

    [Header("Sons")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private void Reset()
    {
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        SetNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SetHighlightState();
        AudioManager.Instance?.PlayUISound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormalState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.PlayUISound(clickSound);
    }

    public void OnSelect(BaseEventData eventData)
    {
        SetHighlightState();
        AudioManager.Instance?.PlayUISound(hoverSound);
    }

    private void SetNormalState()
    {
        if (buttonText != null)
            buttonText.color = normalColor;

        if (normalBackgroundGO != null)
            normalBackgroundGO.SetActive(true);

        if (hoverBackgroundGO != null)
            hoverBackgroundGO.SetActive(false);
    }

    private void SetHighlightState()
    {
        if (buttonText != null)
            buttonText.color = highlightedColor;

        if (normalBackgroundGO != null)
            normalBackgroundGO.SetActive(false);

        if (hoverBackgroundGO != null)
            hoverBackgroundGO.SetActive(true);
    }
}
