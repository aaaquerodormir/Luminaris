using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private GameObject confirmationUI;

    private System.Action confirmedAction;

    // Método Start é chamado assim que a cena de GameOver carrega
    private void Start()
    {
        // Garante que o jogo pause e o áudio pare
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

        // Chama o GameFlowManager (que é persistente) para reiniciar o jogo
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.RequestRetry();
        }
        else
        {
            Debug.LogError("[GameOverMenu] GameFlowManager.Instance não foi encontrado!");
        }
    }

    public void ReturnToMainMenu()
    {
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f; // Reseta o tempo
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

    // --- Métodos de Confirmação (Sem alteração) ---
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