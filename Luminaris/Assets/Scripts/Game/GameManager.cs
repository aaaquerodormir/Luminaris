using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro; // Adicionado para TextMeshPro
using UnityEngine.UI; // Adicionado para Image
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public static event System.Action OnGameOver;
    public static event System.Action OnTryAgain;

    [Header("Jogadores")]
    [SerializeField] private PlayerRespawn player1;
    [SerializeField] private PlayerRespawn player2;

    [Header("Lava")]
    [SerializeField] private LavaRise lava;

    [Header("Controle de Turnos")]
    [SerializeField] private TurnControl turnControl;

    [Header("UI Geral")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private GameObject victoryMenuWrapper;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject confirmationUI;

    [Header("UI de Turno")]
    [Tooltip("O painel que aparece brevemente quando o turno muda.")]
    [SerializeField] private GameObject turnChangePanel;
    [Tooltip("O texto que será atualizado com o nome do jogador.")]
    [SerializeField] private TextMeshProUGUI turnChangeText;
    [Tooltip("A imagem que será atualizada com o sprite do jogador.")]
    [SerializeField] private Image turnChangeImage;
    [Tooltip("Quantos segundos o painel de troca de turno fica na tela.")]
    [SerializeField] private float turnPanelDisplayDuration = 2.5f;

    [Header("UI Específica")]
    [SerializeField] private GameObject jumpCounterUI;

    private bool isGameOver = false;
    //public bool IsGameOverActive => isGameOver;

    private GameSession session;
    private Checkpoint lastCheckpoint;
    private System.Action confirmedAction;

    //public PlayerRespawn GetPlayer1() => player1;
    //public PlayerRespawn GetPlayer2() => player2;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            session = new GameSession();
            //Debug.Log("[GameManager] Instância criada.");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += OnAnyPlayerDeath;
        TurnControl.OnTurnStarted += HandleTurnStart;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= OnAnyPlayerDeath;
        TurnControl.OnTurnStarted -= HandleTurnStart;
    }

    private void Start()
    {
        //Debug.Log($"[GameManager] Start — IsServer={IsServer}, IsClient={IsClient}, IsHost={IsHost}");
    }

    // ==========================
    // ==== MORTE GLOBAL ========
    // ==========================
    private void OnAnyPlayerDeath()
    {
        if (!IsServer)
        {
            //Debug.Log("[GameManager] Cliente detectou morte (ignorado).");
            return;
        }

        //Debug.Log("[GameManager] Um jogador morreu — acionando GameOver para todos.");
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

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        if (turnChangePanel != null)
            turnChangePanel.SetActive(false);

        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    [ClientRpc]
    public void ShowVictoryClientRpc()
    {
        Debug.Log("[GameManager] 🎊 Vitória global recebida — exibindo painel em todos os clientes.");

        if (victoryUI != null) victoryUI.SetActive(true);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (turnChangePanel != null) turnChangePanel.SetActive(false);

        Time.timeScale = 0f;
    }

    // ==========================
    // ==== TURNOS ==============
    // ==========================
    private void HandleTurnStart(PlayerMovement newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogWarning("[GameManager] Turno iniciado com Player nulo!");
            return;
        }

        Debug.Log($"[GameManager] Novo turno iniciado: {newPlayer.name}");
        StartCoroutine(ShowTurnPanelRoutine(newPlayer));
    }

    private IEnumerator ShowTurnPanelRoutine(PlayerMovement playerToShow)
    {
        if (turnChangePanel == null || playerToShow == null)
            yield break;

        PlayerIdentifier id = playerToShow.GetComponent<PlayerIdentifier>();

        if (id != null)
        {
            if (turnChangeText != null)
                turnChangeText.text = $"Agora é a vez da {id.PlayerName}";

            if (turnChangeImage != null)
            {
                turnChangeImage.sprite = id.PlayerSprite;
                turnChangeImage.gameObject.SetActive(id.PlayerSprite != null);
            }
        }

        //Debug.Log($"[GameManager] Exibindo painel de turno para {playerToShow.name}");
        turnChangePanel.SetActive(true);
        yield return new WaitForSeconds(turnPanelDisplayDuration);
        turnChangePanel.SetActive(false);
    }

    // ==========================
    // ==== REINICIAR ===========
    // ==========================
    public void TryAgain()
    {
        Debug.Log("[GameManager] Reiniciando partida.");

        if (gameOverUI != null) gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        if (player1 != null) player1.Respawn();
        if (player2 != null) player2.Respawn();

        if (turnControl != null)
            turnControl.ResetTurns();

        if (pauseMenu != null) pauseMenu.gameObject.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(true);

        isGameOver = false;
        OnTryAgain?.Invoke();
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