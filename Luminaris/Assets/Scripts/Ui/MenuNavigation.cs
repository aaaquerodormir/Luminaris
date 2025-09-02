using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuNavigation : MonoBehaviour
{
    [SerializeField] private GameObject starIndicator; // Sua seta
    [SerializeField] private Button[] buttons;         // Lista dos bot�es
    [SerializeField] private Vector3 offset = new Vector3(-50, 0, 0);

    private int currentIndex = 0;

    void Start()
    {
        UpdateIndicatorPosition();
    }

    void Update()
    {
        // Navega��o simples com setas do teclado para testar
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentIndex = (currentIndex + 1) % buttons.Length;
            UpdateIndicatorPosition();
            EventSystem.current.SetSelectedGameObject(buttons[currentIndex].gameObject);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentIndex = (currentIndex - 1 + buttons.Length) % buttons.Length;
            UpdateIndicatorPosition();
            EventSystem.current.SetSelectedGameObject(buttons[currentIndex].gameObject);
        }
    }

    public void UpdateIndicatorPosition()
    {
        // Move a seta para a posi��o do bot�o atual + offset
        starIndicator.transform.position = (Vector3)buttons[currentIndex].transform.position + offset;
        starIndicator.SetActive(true);
    }

    // Caso queira detectar o mouse passando por cima dos bot�es
    public void OnButtonHover(int buttonIndex)
    {
        currentIndex = buttonIndex;
        UpdateIndicatorPosition();
    }
}
