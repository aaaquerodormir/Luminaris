using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PauseMenu : NetworkBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject confirmationUI;

    [Header("Input Action")]
    [SerializeField] private InputActionReference pauseAction;

    private NetworkVariable<bool> isPaused_Global = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private System.Action confirmedAction;

    public override void OnNetworkSpawn()
    {
        isPaused_Global.OnValueChanged += OnPauseStateChanged;

        UpdatePauseVisuals(isPaused_Global.Value);
    }

    public override void OnNetworkDespawn()
    {
        isPaused_Global.OnValueChanged -= OnPauseStateChanged;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePressed;
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Disable();
            pauseAction.action.performed -= OnPausePressed;
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {

        TogglePauseServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TogglePauseServerRpc()
    {
        isPaused_Global.Value = !isPaused_Global.Value;
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestPauseStateServerRpc(bool shouldBePaused)
    {
        isPaused_Global.Value = shouldBePaused;
    }

    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        UpdatePauseVisuals(newValue);
    }
    private void UpdatePauseVisuals(bool isPaused)
    {
        pauseUI.SetActive(isPaused);
        if (optionsUI != null) optionsUI.SetActive(false);
        if (confirmationUI != null) confirmationUI.SetActive(false);

        if (isPaused)
        {
            AudioManager.Instance.PauseAllLoops();
        }
        else
        {
            AudioManager.Instance.ResumeAllLoops();
        }
        if (IsServer)
        {
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }
    public void Resume()
    {
        RequestPauseStateServerRpc(false);
    }
    public void OpenOptions()
    {
        if (optionsUI != null)
        {
            optionsUI.SetActive(true);
            pauseUI.SetActive(false);
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
            Time.timeScale = 1f;
            if (IsServer)
            {
                LoadMenuAndShutdown();
            }
            else
            {
                RequestReturnToMenuServerRpc();
            }
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReturnToMenuServerRpc()
    {
        LoadMenuAndShutdown();
    }

    private void LoadMenuAndShutdown()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;

        NetworkManager.Singleton.SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {

        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
            sceneEvent.ClientId == NetworkManager.ServerClientId)
        {
            NetworkManager.Singleton.Shutdown();

            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }
    public void QuitGame()
    {
        OpenConfirmation(() =>
        {
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