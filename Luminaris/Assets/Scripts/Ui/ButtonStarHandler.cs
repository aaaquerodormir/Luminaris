using UnityEngine;
using UnityEngine.EventSystems;

// Garante que o script esteja em um objeto com um RectTransform
[RequireComponent(typeof(RectTransform))]
public class ButtonStarHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform buttonRect;

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }

    // Chamado quando o mouse entra na área do botão
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (StarIndicatorController.Instance != null)
        {
            // Move a estrela para a posição deste botão
            StarIndicatorController.Instance.ShowAndFollow(buttonRect);
        }
    }

    // Chamado quando o mouse sai da área do botão
    public void OnPointerExit(PointerEventData eventData)
    {
        if (StarIndicatorController.Instance != null)
        {
            // Esconde a estrela
            StarIndicatorController.Instance.Hide();
        }
    }
}
