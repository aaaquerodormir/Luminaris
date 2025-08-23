using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject pauseUI;   // Painel do pause
    [SerializeField] private GameObject optionsUI; // Subpainel de opções

    private bool isPaused = false;

    private void Update()
    {
        // Só permite pausar se não estiver em Game Over
        if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.Instance.IsGameOverActive)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (GameManager.Instance.IsGameOverActive) return; //  trava pause durante Game Over

        pauseUI.SetActive(true);
        if (optionsUI != null) optionsUI.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Resume()
    {
        pauseUI.SetActive(false);
        if (optionsUI != null) optionsUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    public void OpenOptions()
    {
        if (optionsUI != null) optionsUI.SetActive(true);
    }

    public void CloseOptions()
    {
        if (optionsUI != null) optionsUI.SetActive(false);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
