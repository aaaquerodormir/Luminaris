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

    [Header("Input Action")] // 2. Adicione um campo para a Action
    [SerializeField] private InputActionReference pauseAction;

    private NetworkVariable<bool> isPaused_Global = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private System.Action confirmedAction;

    public override void OnNetworkSpawn()
    {
        // Inscreva-se na mudança de valor. Isso será chamado em todos os peers
        // quando o valor for alterado no servidor.
        isPaused_Global.OnValueChanged += OnPauseStateChanged;

        // Atualiza o estado inicial para caso alguém entre em um jogo já pausado.
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
            // A entrada ainda é local, mas a *ação* será um RPC.
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
        // Não faz nada localmente, exceto pedir ao servidor para alternar o estado.
        // O valor atual de 'isPaused_Global.Value' será usado pelo servidor.
        TogglePauseServerRpc();
    }

    // 6. [ServerRpc] - Executado pelo Client, mas roda NO SERVIDOR.
    // RequireOwnership = false permite que qualquer client chame isso neste objeto (que é um objeto de cena, não do jogador).
    [ServerRpc(RequireOwnership = false)]
    private void TogglePauseServerRpc()
    {
        // O servidor inverte o valor da NetworkVariable.
        isPaused_Global.Value = !isPaused_Global.Value;
    }

    // 6b. [ServerRpc] - Para o botão "Resume"
    [ServerRpc(RequireOwnership = false)]
    private void RequestPauseStateServerRpc(bool shouldBePaused)
    {
        isPaused_Global.Value = shouldBePaused;
    }

    // 7. Evento de Callback - Chamado em TODOS os peers quando isPaused_Global.Value muda.
    private void OnPauseStateChanged(bool previousValue, bool newValue)
    {
        // Todos os peers (Host e Clients) atualizam sua UI e áudio.
        UpdatePauseVisuals(newValue);
    }

    // 8. Lógica de atualização local (agora separada da lógica de rede)
    private void UpdatePauseVisuals(bool isPaused)
    {
        pauseUI.SetActive(isPaused);
        if (optionsUI != null) optionsUI.SetActive(false); // Sempre feche as opções
        if (confirmationUI != null) confirmationUI.SetActive(false); // E a confirmação

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

    // 10. O botão "Resume" na UI deve chamar esta função.
    public void Resume()
    {
        // Solicita ao servidor para despausar.
        RequestPauseStateServerRpc(false);
    }

    // ----- Funções de UI Locais (Opções, Confirmação) -----
    // (Estas não precisam de rede, pois são painéis dentro do menu de pausa)

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

    // ----- Lógica de Transição de Cena de Rede -----

    public void ReturnToMenu()
    {
        OpenConfirmation(() =>
        {
            // Resetar o timeScale localmente é bom, especialmente para o Host.
            Time.timeScale = 1f;

            // 11. O Host executa a lógica, o Client envia um RPC para o Host executá-la.
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
        // Um Client chamou, agora o Servidor executa a lógica.
        LoadMenuAndShutdown();
    }

    private void LoadMenuAndShutdown()
    {
        // Proteção extra para garantir que só o servidor execute
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
        // Estamos interessados apenas no evento "LoadComplete" (carregamento concluído)
        // E especificamente quando o SERVIDOR (Host) é quem terminou de carregar.

        // CORREÇÃO: ServerClientId é estático, então usamos NetworkManager.ServerClientId
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete &&
            sceneEvent.ClientId == NetworkManager.ServerClientId)
        {
            // 5. AGORA que o servidor carregou o Menu, é seguro desligar.
            // Os clients já receberam a instrução e estão a caminho.
            NetworkManager.Singleton.Shutdown();

            // 6. (Importante) Limpe a inscrição do evento para evitar que
            // esta função seja chamada novamente no futuro (memory leak).
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    // A lógica de QuitGame está correta como uma ação local.
    // Cada jogador desliga sua própria conexão e fecha seu próprio app.
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

    // ----- Funções de Confirmação (Locais) -----

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
