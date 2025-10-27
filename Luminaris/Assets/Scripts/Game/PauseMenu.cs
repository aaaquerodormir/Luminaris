using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject confirmationUI;

    [Header("Input Action")] // 2. Adicione um campo para a Action
    [SerializeField] private InputActionReference pauseAction;

    private bool isPaused = false;
    private System.Action confirmedAction;

    private void OnEnable()
    {
        // Garante que a action existe e a ativa
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            // Registra a função OnPausePressed para ser chamada 
            // quando a action for 'performed' (pressionada)
            pauseAction.action.performed += OnPausePressed;
        }
    }

    private void OnDisable()
    {
        // Limpa o registro e desativa a action
        if (pauseAction != null)
        {
            pauseAction.action.Disable();
            pauseAction.action.performed -= OnPausePressed;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        // Coloque sua lógica de "Game Over" aqui se precisar
        // if (GameManager.Instance.IsGameOverActive) return;

        // Inverte o estado de pausa
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Pause()
    {
        //if (GameManager.Instance.IsGameOverActive) return;

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
        if (optionsUI != null)
        {
            optionsUI.SetActive(true);
            pauseUI.SetActive(false);

            // Deixa o OpcoesMenu aplicar os valores do save via OnEnable
            Debug.Log("[PauseMenu] Painel de opções aberto.");
        }
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
            // 2. Sempre resetar o Time.timeScale ANTES de sair da cena.
            // (Isso evita que seu Menu fique congelado se você saiu pausado)
            Time.timeScale = 1f;

            // 3. Chamar o Shutdown ANTES de carregar a cena
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // 4. Agora é seguro carregar o menu localmente
            SceneManager.LoadScene("Menu");
        });
    }

    public void QuitGame()
    {
        OpenConfirmation(() =>
        {
            // 5. Chamar o Shutdown antes de fechar o aplicativo
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.Shutdown();
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
