using UnityEngine;
using UnityEngine.SceneManagement;

public class BotaoVoltar : MonoBehaviour
{
    [Tooltip("O nome da cena do Menu Principal.")]
    [SerializeField] private string menuSceneName = "Menu";
    public void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            return;
        }
        SceneManager.LoadScene(menuSceneName);
    }
}
