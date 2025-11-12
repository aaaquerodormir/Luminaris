using UnityEngine;
using UnityEngine.SceneManagement;

public class BotaoVoltar : MonoBehaviour
{
    [Tooltip("O nome da cena do Menu Principal.")]
    [SerializeField] private string menuSceneName = "Menu";
    public void LoadMenuScene()
    {
        // Garante que o nome da cena está correto
        if (string.IsNullOrEmpty(menuSceneName))
        {
            return;
        }

        // Carrega a cena do Menu Principal
        SceneManager.LoadScene(menuSceneName);
    }
}
