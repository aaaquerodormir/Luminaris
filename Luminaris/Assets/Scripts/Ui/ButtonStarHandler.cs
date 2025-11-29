using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonStarHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform buttonRect;

    private void Awake()
    {
        buttonRect = GetComponent<RectTransform>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (StarIndicatorController.Instance != null)
        {
            StarIndicatorController.Instance.ShowAndFollow(buttonRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (StarIndicatorController.Instance != null)
        {
            StarIndicatorController.Instance.Hide();
        }
    }
}
