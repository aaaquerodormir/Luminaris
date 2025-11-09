using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public static event System.Action OnGameOver;

    [Header("Jogadores")]
    [SerializeField] private PlayerRespawn player1;
    [SerializeField] private PlayerRespawn player2;

    [Header("Lava")]
    [SerializeField] private LavaRise lava;

    [Header("Controle de Turnos")]
    [SerializeField] private TurnControl turnControl; // A referência ainda é útil

    [Header("UI Geral")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private GameObject victoryMenuWrapper;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject confirmationUI;

    // --- SEÇÃO "UI de Turno" REMOVIDA ---
    // [Header("UI de Turno")]
    // ... (variáveis movidas para TurnControl)

    [Header("UI Específica")]
    [SerializeField] private GameObject jumpCounterUI;

    private bool isGameOver = false;
    private GameSession session;
    private Checkpoint lastCheckpoint;
    private System.Action confirmedAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            session = new GameSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += OnAnyPlayerDeath;
        // --- REMOVIDO: GameManager não escuta mais os turnos ---
        // TurnControl.OnTurnStarted += HandleTurnStart; 
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= OnAnyPlayerDeath;
        // --- REMOVIDO ---
        // TurnControl.OnTurnStarted -= HandleTurnStart;
    }

    private void Start() { }

    // ==========================
    // ==== MORTE GLOBAL ========
    // ==========================
    private void OnAnyPlayerDeath()
    {
        if (!IsServer) return;
        ShowGameOverClientRpc();
    }

    [ClientRpc]
    private void ShowGameOverClientRpc()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] Exibindo tela de Game Over.");
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(true);

        // --- ALTERAÇÃO: Notifica o TurnControl para se limpar ---
        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    [ClientRpc]
    public void ShowVictoryClientRpc()
    {
        Debug.Log("[GameManager] 🎊 Vitória global recebida.");

        if (victoryUI != null) victoryUI.SetActive(true);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);

        // --- ALTERAÇÃO: Notifica o TurnControl para se limpar ---
        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        Time.timeScale = 0f;
    }

    // ==========================
    // ==== TURNOS (REMOVIDO) ===
    // ==========================

    // --- MÉTODO HandleTurnStart REMOVIDO ---
    // --- CORROTINA ShowTurnPanelRoutine REMOVIDA ---

    // ==========================
    // ==== REINICIAR ===========
    // ==========================

    public void RequestRetryGame()
    {
        Debug.Log("[GameManager] Recebida solicitação de Retry. Enviando ao servidor.");
        RequestRetryGameServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRetryGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        Debug.Log($"[GameManager-SERVER] Cliente {rpcParams.Receive.SenderClientId} requisitou reinício.");
        ResetStateAndReloadClientRpc();
    }

    [ClientRpc]
    private void ResetStateAndReloadClientRpc()
    {
        Debug.Log($"[GameManager-CLIENT {NetworkManager.Singleton.LocalClientId}] Recebido comando de Reset.");

        Time.timeScale = 1f;
        isGameOver = false;

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(true);
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(false);

        // --- ALTERAÇÃO: Notifica o TurnControl para se limpar ---
        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        if (IsServer)
        {
            Debug.Log("[GameManager-SERVER] Estado local resetado. Solicitando transição de cena.");
            var sceneName = SceneManager.GetActiveScene().name;
            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.TransitionToScene(sceneName);
            }
            else
            {
                Debug.LogError("[GameManager-SERVER] GameFlowManager.Instance é nulo!");
            }
        }
    }

    // ==========================
    // ==== GAME OVER RPC =======
    // ==========================
    [ServerRpc(RequireOwnership = false)]
    public void InvokeGameOverServerRpc()
    {
        if (!IsServer) return;
        Debug.Log("[GameManager] Um jogador morreu — acionando GameOver para todos.");
        ShowGameOverClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerDiedServerRpc()
    {
        ShowGameOverClientRpc();
    }
}