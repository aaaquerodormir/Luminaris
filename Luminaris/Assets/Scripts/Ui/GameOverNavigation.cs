using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameOverNavigation : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    private int currentIndex = -1;

    void Start()
    {
        HighlightButton(-1);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1 + buttons.Length) % buttons.Length;
            HighlightButton(currentIndex);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
            HighlightButton(currentIndex);
        }
    }

    private void HighlightButton(int index)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform leftStar = buttons[i].transform.GetChild(0);
            Transform textObj = buttons[i].transform.GetChild(1);
            Transform rightStar = buttons[i].transform.GetChild(2); 

            bool active = (i == index);

            if (leftStar) leftStar.gameObject.SetActive(active);
            if (rightStar) rightStar.gameObject.SetActive(active);
        }

        if (index >= 0 && index < buttons.Length)
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        else
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnButtonHover(int buttonIndex)
    {
        currentIndex = buttonIndex;
        HighlightButton(currentIndex);
    }

    public void OnButtonExit(int buttonIndex)
    {
        if (currentIndex == buttonIndex)
        {
            HighlightButton(-1);
            currentIndex = -1;
        }
    }

    public void OnButtonClick()
    {
        HighlightButton(-1);
        currentIndex = -1;
    }
}
