using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private GameObject confirmationUI;

    private System.Action confirmedAction;

    public void TryAgain()
    {
        // Bot�o "Tentar Novamente"
        gameManager.TryAgain();
    }

    public void ReturnToMainMenu()
    {
        // Mostra confirma��o antes de voltar ao menu
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        });
    }

    public void QuitGame()
    {
        // Mostra confirma��o antes de sair
        OpenConfirmation(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private void OpenConfirmation(System.Action action)
    {
        confirmationUI.SetActive(true);
        confirmedAction = action;
    }

    public void Confirm()
    {
        confirmationUI.SetActive(false);
        confirmedAction?.Invoke();
    }

    public void Cancel()
    {
        confirmationUI.SetActive(false);
        confirmedAction = null;
    }
}
