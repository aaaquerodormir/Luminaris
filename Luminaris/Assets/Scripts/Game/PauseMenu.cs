using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject confirmationUI;

    private bool isPaused = false;
    private System.Action confirmedAction;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.Instance.IsGameOverActive)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        if (GameManager.Instance.IsGameOverActive) return;

        pauseUI.SetActive(true);
        if (optionsUI != null) optionsUI.SetActive(false);

        Time.timeScale = 0f;
        isPaused = true;

        AudioManager.Instance.PauseAllLoops();
    }

    public void Resume()
    {
        pauseUI.SetActive(false);
        if (optionsUI != null) optionsUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        AudioManager.Instance.ResumeAllLoops();
    }

    public void OpenOptions()
    {
        if (optionsUI != null) optionsUI.SetActive(true);
        if (pauseUI != null) pauseUI.SetActive(false);
    }

    public void CloseOptions()
    {
        if (optionsUI != null) optionsUI.SetActive(false);
        if (pauseUI != null) pauseUI.SetActive(true);
    }

    public void ReturnToMenu()
    {
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        });
    }

    public void QuitGame()
    {
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
