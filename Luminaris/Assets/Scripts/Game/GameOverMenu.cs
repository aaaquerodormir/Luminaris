using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private GameObject confirmationUI;

    private System.Action confirmedAction;

    private void Start()
    {
        Time.timeScale = 0f;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PauseAllLoops();
        }
    }
    public void TryAgain()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ResumeAllLoops();
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RequestRetry();
        }
        else
        {
        }
    }
    public void ReturnToMainMenu()
    {
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResumeAllLoops();
            }

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene(mainMenuScene);
        });
    }
    public void QuitGame()
    {
        OpenConfirmation(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PauseAllLoops();
            }
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