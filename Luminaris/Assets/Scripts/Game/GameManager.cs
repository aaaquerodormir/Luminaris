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
    //private System.Action confirmedAction;

    //public PlayerRespawn GetPlayer1() => player1;
    //public PlayerRespawn GetPlayer2() => player2;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        session = new GameSession();
        Debug.Log("[GameManager] Awake — Instância criada");
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += HandlePlayerDeath;
        TurnControl.OnTurnStarted += HandleTurnStart;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= HandlePlayerDeath;
        TurnControl.OnTurnStarted -= HandleTurnStart;
    }

    private void HandlePlayerDeath()
    {
        if (!IsServer)
        {
            Debug.Log("[GameManager] Cliente detectou morte — ignorando");
            return;
        }

        Debug.Log("[GameManager] Jogador morreu — executando GameOver no servidor");
        ShowGameOverClientRpc();
    }

    private void HandleTurnStart(PlayerMovement newPlayer)
    {
        Debug.Log($"[GameManager] Novo turno iniciado: {newPlayer.name}");
        StartCoroutine(ShowTurnPanelRoutine(newPlayer));
    }

    private IEnumerator ShowTurnPanelRoutine(PlayerMovement playerToShow)
    {
        if (turnChangePanel == null || playerToShow == null)
            yield break;

        PlayerIdentifier id = playerToShow.GetComponent<PlayerIdentifier>();
        if (id != null && turnChangeText != null)
        {
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

    [ClientRpc]
    private void ShowGameOverClientRpc()
    {
        Debug.Log("[GameManager] Exibindo GameOver em todos os clientes");

        if (isGameOver) return;
        isGameOver = true;

        pauseMenu?.gameObject.SetActive(false);
        hudContainer?.SetActive(false);
        victoryUI?.SetActive(false);
        victoryMenuWrapper?.SetActive(false);
        gameOverUI.SetActive(true);
        turnChangePanel?.SetActive(false);

        Time.timeScale = 0f;
        OnGameOver?.Invoke();
    }

    public void TryAgain()
    {
        Debug.Log("[GameManager] Reiniciando partida...");

        gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        player1.Respawn();
        player2.Respawn();

        if (lastCheckpoint != null)
            lava.ResetLava(lastCheckpoint);

        turnControl.ResetTurns();
        session.ResetSession();

        pauseMenu?.gameObject.SetActive(true);
        hudContainer?.SetActive(true);

        isGameOver = false;
        OnTryAgain?.Invoke();
    }

    public void ReachCheckpoint(Transform checkpointTransform)
    {
        if (!IsServer) return;

        var cp = checkpointTransform.GetComponent<Checkpoint>();
        if (cp == null) return;

        Debug.Log("[GameManager] Checkpoint atingido — verificando progresso");

        var p1Pending = player1.GetPendingCheckpoint();
        var p2Pending = player2.GetPendingCheckpoint();

        if (p1Pending != null && p2Pending != null && p1Pending.GroupId == p2Pending.GroupId)
        {
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
}