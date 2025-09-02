using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private GameObject starIndicator;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Vector3 offset = new Vector3(-50, 0, 0);

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

        starIndicator.transform.position = buttons[currentIndex].transform.position + offset;
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
        // Só desativa se o botão que perdeu hover for o mesmo que estava selecionado
        if (currentIndex == buttonIndex)
        {
            currentIndex = -1;
            starIndicator.SetActive(false);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
