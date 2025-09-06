using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private GameObject starIndicator;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Vector2 offset = new Vector2(-50, 0);

    private int currentIndex = -1;

    void Start()
    {
        starIndicator.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1 + buttons.Length) % buttons.Length;
            ShowIndicator();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
            ShowIndicator();
        }
    }

    private void ShowIndicator()
    {
        if (currentIndex < 0 || currentIndex >= buttons.Length) return;

        RectTransform starRect = starIndicator.GetComponent<RectTransform>();
        RectTransform buttonRect = buttons[currentIndex].GetComponent<RectTransform>();

        starRect.anchoredPosition = buttonRect.anchoredPosition + offset;

        if (!starIndicator.activeSelf)
            starIndicator.SetActive(true);

        EventSystem.current.SetSelectedGameObject(buttons[currentIndex].gameObject);
    }

    public void OnButtonHover(int buttonIndex)
    {
        currentIndex = buttonIndex;
        ShowIndicator();
    }

    public void OnButtonExit(int buttonIndex)
    {
        if (currentIndex == buttonIndex)
        {
            currentIndex = -1;
            starIndicator.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnButtonClick()
    {
        starIndicator.SetActive(false);
        currentIndex = -1;
        EventSystem.current.SetSelectedGameObject(null);
    }
}
