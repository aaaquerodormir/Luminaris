using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Precisamos disso para Coroutines
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public static event System.Action OnGameOver;

    [Header("Lava")]
    [SerializeField] private LavaRise lava;

    [Header("Controle de Turnos")]
    [SerializeField] private TurnControl turnControl;

    [Header("Controle de Cenas")]
    [Tooltip("O nome da cena de GameOver para onde o jogo deve transicionar.")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";

    [Header("UI Geral")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject confirmationUI;

    [Header("Transições")]
    [Tooltip("Painel de Imagem preto para cobrir a tela no Game Over")]
    [SerializeField] private GameObject faderPanel;

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
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= OnAnyPlayerDeath;
    }

    // --- MORTE GLOBAL ---
    private void OnAnyPlayerDeath()
    {
        if (!IsServer || isGameOver) return; // Trava do servidor (Correto)
        isGameOver = true; // Servidor se trava para não repetir

        // 1. Manda clientes ativarem as proteções
        PauseGameAndEnableTransitionSafeguardsClientRpc(); // Envia o RPC (só uma vez)

        // 2. Inicia Coroutine para dar tempo ao RPC
        StartCoroutine(DelayedSceneTransition());
    }

    private IEnumerator DelayedSceneTransition()
    {
        // Delay para garantir que o RPC chegue e ative o fader.
        // 0.5s é seguro, mas você pode tentar 0.2s se quiser.
        yield return new WaitForSeconds(0.5f);

        // 3. AGORA, com tudo protegido, mudamos de cena
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TransitionToScene(gameOverSceneName, false);
        }
        else
        {
            Debug.LogError("[GameManager] GameFlowManager.Instance é nulo!");
        }
    }


    [ClientRpc]
    private void PauseGameAndEnableTransitionSafeguardsClientRpc()
    {
        // --- A CORREÇÃO ESTÁ AQUI ---
        // O servidor já tem a trava "isGameOver", então este RPC só será enviado UMA VEZ.
        // A trava "if (isGameOver) return;" aqui estava fazendo o HOST
        // (que é o servidor) pular o código.

        // if (isGameOver) return; // <-- REMOVA ESTA LINHA
        // isGameOver = true; // <-- REMOVA ESTA LINHA

        // --- FIM DA CORREÇÃO ---


        // Limpa a UI
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);

        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        // --- SOLUÇÃO 1: O "SUSPENSÓRIO" (VISUAL) ---
        if (faderPanel != null)
        {
            faderPanel.SetActive(true);
            Debug.Log($"[GameManager] FaderPanel ativado no Client ID: {NetworkManager.Singleton.LocalClientId}");
        }
        else
        {
            Debug.LogWarning("[GameManager] faderPanel é nulo.");
        }

        // --- SOLUÇÃO 2: O "CINTO" (TÉCNICO) ---
        GameObject[] fallbackCameras = GameObject.FindGameObjectsWithTag("FallbackCamera");

        if (fallbackCameras.Length > 0)
        {
            foreach (GameObject cam in fallbackCameras)
            {
                cam.SetActive(true);
            }
            Debug.Log($"[GameManager] {fallbackCameras.Length} câmera(s) de fallback REATIVADA(S).");
        }
        else
        {
            Debug.LogWarning("[GameManager] Nenhuma câmera com a tag 'FallbackCamera' foi encontrada!");
        }
        // --- FIM DAS SOLUÇÕES ---

        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    // --- RPCs DE GAME OVER ---
    [ServerRpc(RequireOwnership = false)]
    public void InvokeGameOverServerRpc()
    {
        if (!IsServer) return;
        OnAnyPlayerDeath(); // Chama a lógica unificada
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerDiedServerRpc()
    {
        if (!IsServer) return;
        OnAnyPlayerDeath(); // Chama a lógica unificada
    }
}