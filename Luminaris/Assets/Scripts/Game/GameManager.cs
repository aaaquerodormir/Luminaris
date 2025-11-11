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

    // --- SOLUÇÃO COMBINADA ---
    [Header("Transições")]
    [Tooltip("Painel de Imagem preto para cobrir a tela no Game Over")]
    [SerializeField] private GameObject faderPanel; // O "suspensório" (solução visual)
    // Não precisamos de um campo para a fallback camera, pois a encontramos pela tag.
    // --- FIM DA SOLUÇÃO ---

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
        if (!IsServer || isGameOver) return; // Proteção
        isGameOver = true;

        // 1. Manda clientes ativarem AMBAS as proteções
        PauseGameAndEnableTransitionSafeguardsClientRpc();

        // 2. Inicia Coroutine para dar tempo ao RPC e à destruição das câmeras dos jogadores
        StartCoroutine(DelayedSceneTransition());
    }

    private IEnumerator DelayedSceneTransition()
    {
        // Aumentamos o tempo de espera para 0.5s.
        // Isso dá tempo suficiente para o RPC ser processado, a FallbackCamera ser ativada
        // e a destruição/desativação das câmeras dos jogadores ser concluída.
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
    // Nome do método atualizado para refletir ambas as ações
    private void PauseGameAndEnableTransitionSafeguardsClientRpc()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Limpa a UI
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);

        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        // --- SOLUÇÃO 1: O "SUSPENSÓRIO" (VISUAL) ---
        if (faderPanel != null)
        {
            faderPanel.SetActive(true);
            Debug.Log("[GameManager] FaderPanel ativado.");
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