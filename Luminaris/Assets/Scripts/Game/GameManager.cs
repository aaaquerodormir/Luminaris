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
            Debug.Log("[GameManager] Instância criada com sucesso.");
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
        Debug.Log($"[GameManager] Start — IsServer={IsServer}, IsClient={IsClient}, IsHost={IsHost}");
    }

    // ==========================
    // ==== MORTE GLOBAL ========
    // ==========================
    private void OnAnyPlayerDeath()
    {
        if (!IsServer)
        {
            Debug.Log("[GameManager] Cliente detectou morte (ignorado, apenas o servidor executa GameOver).");
            return;
        }

        Debug.Log("[GameManager] Um jogador morreu — acionando GameOver para todos.");
        ShowGameOverClientRpc();
    }

    [ClientRpc]
    private void ShowGameOverClientRpc()
    {
        if (isGameOver) return;
        isGameOver = true;

        Debug.Log("[GameManager] Exibindo tela de Game Over em todos os clientes.");

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

    // ==========================
    // ==== TURNOS ============
    // ==========================
    private void HandleTurnStart(PlayerMovement newPlayer)
    {
        if (newPlayer == null)
        {
            Debug.LogWarning("[GameManager] Turno iniciado, mas PlayerMovement é nulo!");
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

        turnChangePanel.SetActive(true);
        yield return new WaitForSeconds(turnPanelDisplayDuration);
        turnChangePanel.SetActive(false);
    }

    // ==========================
    // ==== CHECKPOINT ==========
    // ==========================
    public void ReachCheckpoint(Transform checkpointTransform)
    {
        if (!IsServer)
        {
            Debug.Log("[GameManager] Cliente tentou registrar checkpoint (ignorado).");
            return;
        }

        var cp = checkpointTransform.GetComponent<Checkpoint>();
        if (cp == null) return;

        Debug.Log("[GameManager] Checkpoint atingido — verificando progresso global.");

        var p1Pending = player1 != null ? player1.GetPendingCheckpoint() : null;
        var p2Pending = player2 != null ? player2.GetPendingCheckpoint() : null;

        if (p1Pending != null && p2Pending != null && p1Pending.GroupId == p2Pending.GroupId)
        {
            Debug.Log($"[GameManager] Ambos os jogadores atingiram o mesmo checkpoint {cp.GroupId}.");

            player1.CommitPendingCheckpoint();
            player2.CommitPendingCheckpoint();
            lastCheckpoint = p1Pending;

            lava.SaveProgressAtCheckpoint();

            float safeZone = Mathf.Min(
                player1.GetCommittedCheckpoint().LavaSafeHeight,
                player2.GetCommittedCheckpoint().LavaSafeHeight
            );

            lava.SetSafeZone(safeZone);
        }
    }

    // ==========================
    // ==== REINICIAR ==========
    // ==========================
    public void TryAgain()
    {
        Debug.Log("[GameManager] Reiniciando partida (TryAgain).");

        if (gameOverUI != null) gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        if (player1 != null) player1.Respawn();
        if (player2 != null) player2.Respawn();

        if (lastCheckpoint != null && lava != null)
            lava.ResetLava(lastCheckpoint);

        if (turnControl != null)
            turnControl.ResetTurns();

        session?.ResetSession();

        if (pauseMenu != null) pauseMenu.gameObject.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(true);

        isGameOver = false;
        OnTryAgain?.Invoke();
    }

    // ==========================
    // ==== VITÓRIA ============
    // ==========================
    [ClientRpc]
    public void ShowVictoryPanelClientRpc()
    {
        Debug.Log("[GameManager] Exibindo tela de vitória em todos os clientes.");

        if (gameOverUI != null) gameOverUI.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(true);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(true);

        if (victoryUI != null && victoryUI.TryGetComponent(out Animator anim))
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (pauseMenu != null) pauseMenu.gameObject.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(true);
        if (jumpCounterUI != null) jumpCounterUI.SetActive(false);

        AudioManager.Instance.PauseAllLoops();
        Time.timeScale = 0f;
    }
}