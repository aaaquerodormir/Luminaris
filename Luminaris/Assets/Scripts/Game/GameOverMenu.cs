using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    //[SerializeField] private GameManager gameManager;
    [SerializeField] private string mainMenuScene = "Menu";
    [SerializeField] private GameObject confirmationUI;

    private System.Action confirmedAction;

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        //  pausa todos os loops (lava, etc.)
        AudioManager.Instance.PauseAllLoops();
    }

    public void TryAgain()
    {
        AudioManager.Instance.ResumeAllLoops(); // retoma ao recomeçar

        // Chame o Singleton diretamente.
        // Isto é mais robusto contra recarregamento de cena.
        if (GameManager.Instance != null)
        {
            // Chame o método que inicia o processo de rede
            GameManager.Instance.RequestRetryGame();
        }
        else
        {
            Debug.LogError("[GameOverMenu] GameManager.Instance não foi encontrado!");
        }
    }

    public void ReturnToMainMenu()
    {
        OpenConfirmation(() =>
        {
            Time.timeScale = 1f;
            AudioManager.Instance.ResumeAllLoops();

            // Desconectar ANTES de carregar o menu
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
            }

            // Agora sim, carregar o menu localmente
            SceneManager.LoadScene(mainMenuScene);
        });
    }

    public void QuitGame()
    {
        OpenConfirmation(() =>
        {
            AudioManager.Instance.PauseAllLoops(); // pausa antes de sair
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
