using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string mainMenuScene = "MainMenu";

    public void TentarNovamente()
    {
        gameManager.TentarNovamente();
    }

    public void VoltarMenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void SairDoJogo()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
