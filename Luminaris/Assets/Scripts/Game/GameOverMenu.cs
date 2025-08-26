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
        // Botão "Tentar Novamente"
        gameManager.TryAgain();
    }

    public void ReturnToMainMenu()
    {
        // Mostra confirmação antes de voltar ao menu
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        });
    }

    public void QuitGame()
    {
        // Mostra confirmação antes de sair
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
