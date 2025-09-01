using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class TextHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Cores")]
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color highlightedColor = Color.gray;

    private void Reset()
    {
        // Tenta pegar automaticamente o TMP do filho se estiver vazio
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
        if (buttonText != null)
            buttonText.color = highlightedColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = normalColor;
    }
}
