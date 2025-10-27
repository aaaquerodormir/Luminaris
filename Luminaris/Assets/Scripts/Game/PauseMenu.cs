using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PauseMenu : NetworkBehaviour
{
    [Header("Refer�ncias de UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject optionsUI;
    [SerializeField] private GameObject confirmationUI;

    [Header("Input Action")] // 2. Adicione um campo para a Action
    [SerializeField] private InputActionReference pauseAction;

    private NetworkVariable<bool> isPaused_Global = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private System.Action confirmedAction;

    public override void OnNetworkSpawn()
    {
        // Inscreva-se na mudan�a de valor. Isso ser� chamado em todos os peers
        // quando o valor for alterado no servidor.
        isPaused_Global.OnValueChanged += OnPauseStateChanged;

        // Atualiza o estado inicial para caso algu�m entre em um jogo j� pausado.
        UpdatePauseVisuals(isPaused_Global.Value);
    }

    // 4. Limpe os listeners no OnNetworkDespawn
    public override void OnNetworkDespawn()
    {
        isPaused_Global.OnValueChanged -= OnPauseStateChanged;
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            // A entrada ainda � local, mas a *a��o* ser� um RPC.
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

    // 5. Chamado localmente quando o jogador pressiona "Pause"
    private void OnPausePressed(InputAction.CallbackContext context)
    {
        // N�o faz nada localmente, exceto pedir ao servidor para alternar o estado.
        // O valor atual de 'isPaused_Global.Value' ser� usado pelo servidor.
        TogglePauseServerRpc();
    }

    // 6. [ServerRpc] - Executado pelo Client, mas roda NO SERVIDOR.
    // RequireOwnership = false permite que qualquer client chame isso neste objeto (que � um objeto de cena, n�o do jogador).
    [ServerRpc(RequireOwnership = false)]
    private void TogglePauseServerRpc()
    {
        // O servidor inverte o valor da NetworkVariable.
        isPaused_Global.Value = !isPaused_Global.Value;
    }

    // 6b. [ServerRpc] - Para o bot�o "Resume"
    [ServerRpc(RequireOwnership = false)]
    private void RequestPauseStateServerRpc(bool shouldBePaused)
    {
        isPaused_Global.Value = shouldBePaused;
    }

    // 7. Evento de Callback - Chamado em TODOS os peers quando isPaused_Global.Value muda.
    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        // Todos os peers (Host e Clients) atualizam sua UI e �udio.
        UpdatePauseVisuals(newValue);
    }

    // 8. L�gica de atualiza��o local (agora separada da l�gica de rede)
    private void UpdatePauseVisuals(bool isPaused)
    {
        pauseUI.SetActive(isPaused);
        if (optionsUI != null) optionsUI.SetActive(false); // Sempre feche as op��es
        if (confirmationUI != null) confirmationUI.SetActive(false); // E a confirma��o

        if (isPaused)
        {
            AudioManager.Instance.PauseAllLoops();
        }
        else
        {
            AudioManager.Instance.ResumeAllLoops();
        }

        // 9. APENAS o Host/Servidor deve controlar o Time.timeScale
        if (IsServer)
        {
            Time.timeScale = isPaused ? 0f : 1f;
        }
    }

    // 10. O bot�o "Resume" na UI deve chamar esta fun��o.
    public void Resume()
    {
        // Solicita ao servidor para despausar.
        RequestPauseStateServerRpc(false);
    }

    // ----- Fun��es de UI Locais (Op��es, Confirma��o) -----
    // (Estas n�o precisam de rede, pois s�o pain�is dentro do menu de pausa)

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

    // ----- L�gica de Transi��o de Cena de Rede -----

    public void ReturnToMenu()
    {
        OpenConfirmation(() =>
        {
            // Resetar o timeScale localmente � bom, especialmente para o Host.
            Time.timeScale = 1f;

            // 11. O Host executa a l�gica, o Client envia um RPC para o Host execut�-la.
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
        // Um Client chamou, agora o Servidor executa a l�gica.
        LoadMenuAndShutdown();
    }

    private void LoadMenuAndShutdown()
    {
        // Prote��o extra para garantir que s� o servidor execute
        if (!IsServer) return;

        // 1. INSCREVA-SE no evento de cena ANTES de iniciar o carregamento.
        // Queremos saber quando o SERVIDOR terminou de carregar.
        NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;

        // 2. Use o NetworkSceneManager para que TODOS os clients carreguem a cena.
        // Isso instrui todos os clients conectados a carregar "Menu".
        NetworkManager.Singleton.SceneManager.LoadScene("Menu", LoadSceneMode.Single);
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        // Estamos interessados apenas no evento "LoadComplete" (carregamento conclu�do)
        // E especificamente quando o SERVIDOR (Host) � quem terminou de carregar.

        // CORRE��O: ServerClientId � est�tico, ent�o usamos NetworkManager.ServerClientId
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
            sceneEvent.ClientId == NetworkManager.ServerClientId)
        {
            // 5. AGORA que o servidor carregou o Menu, � seguro desligar.
            // Os clients j� receberam a instru��o e est�o a caminho.
            NetworkManager.Singleton.Shutdown();

            // 6. (Importante) Limpe a inscri��o do evento para evitar que
            // esta fun��o seja chamada novamente no futuro (memory leak).
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    // A l�gica de QuitGame est� correta como uma a��o local.
    // Cada jogador desliga sua pr�pria conex�o e fecha seu pr�prio app.
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

    // ----- Fun��es de Confirma��o (Locais) -----

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
