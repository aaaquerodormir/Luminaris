using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler
{
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Cores")]
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
        if (buttonText != null)
            buttonText.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Highlight();
        AudioManager.Instance.PlayUISound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance.PlayUISound(clickSound);
    }

    // Chamado quando o botão é selecionado via teclado ou controle
    public void OnSelect(BaseEventData eventData)
    {
        Highlight();
        AudioManager.Instance.PlayUISound(hoverSound);
    }

    private void Highlight()
    {
        if (buttonText != null)
            buttonText.color = highlightedColor;
    }
}
