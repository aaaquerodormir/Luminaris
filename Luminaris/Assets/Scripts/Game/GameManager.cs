using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
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

    [Header("UI")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private GameObject victoryMenuWrapper;  // Painel extra da vitória
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject confirmationUI;       // Painel de confirmação (igual PauseMenu)

    [Header("UI Específica")]
    [SerializeField] private GameObject jumpCounterUI; // Contador de pulo

    private bool isGameOver = false;
    public bool IsGameOverActive => isGameOver;

    private GameSession session;
    private Checkpoint lastCheckpoint;

    private System.Action confirmedAction;

    public PlayerRespawn GetPlayer1() => player1;
    public PlayerRespawn GetPlayer2() => player2;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        session = new GameSession();
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += ShowGameOver;
        TurnControl.OnTurnEnded += HandleTurnEnd;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= ShowGameOver;
        TurnControl.OnTurnEnded -= HandleTurnEnd;
    }

    private void HandleTurnEnd()
    {
      
    }

    public void RegisterResettable(IResettable obj)
    {
        session.RegisterResettable(obj);
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);
        if (victoryUI != null) victoryUI.SetActive(false);
        if (victoryMenuWrapper != null) victoryMenuWrapper.SetActive(false);

        gameOverUI.SetActive(true);
        Time.timeScale = 0f;

        OnGameOver?.Invoke();
    }

    public void TryAgain()
    {
        gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        player1.Respawn();
        player2.Respawn();

        if (lastCheckpoint != null)
            lava.ResetLava(lastCheckpoint);

        turnControl.ResetTurns();
        session.ResetSession();

        if (pauseMenu != null) pauseMenu.gameObject.SetActive(true);
        if (hudContainer != null) hudContainer.SetActive(true);

        isGameOver = false;

        OnTryAgain?.Invoke();
    }

    public void ShowVictoryPanel()
    {
        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (victoryUI != null)
            victoryUI.SetActive(true);

        if (victoryMenuWrapper != null)
            victoryMenuWrapper.SetActive(true);

        // Garante que a animação funcione com Time.timeScale = 0
        Animator anim = victoryUI != null ? victoryUI.GetComponent<Animator>() : null;
        if (anim != null)
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(true); // Mantém o PauseMenu ativo para o botão funcionar

        if (hudContainer != null)
            hudContainer.SetActive(true);

        if (jumpCounterUI != null)
            jumpCounterUI.SetActive(false);

        AudioManager.Instance.PauseAllLoops();

        Time.timeScale = 0f;
    }

    public void ReachCheckpoint(Transform checkpointTransform)
    {
        var cp = checkpointTransform.GetComponent<Checkpoint>();
        if (cp == null) return;

        var p1Pending = player1.GetPendingCheckpoint();
        var p2Pending = player2.GetPendingCheckpoint();

        if (p1Pending != null && p2Pending != null && p1Pending.GroupId == p2Pending.GroupId)
        {
            player1.CommitPendingCheckpoint();
            player2.CommitPendingCheckpoint();

            lastCheckpoint = p1Pending;

            SaveData data = new SaveData
            {
                checkpointGroup = lastCheckpoint.GroupId
            };

            if (lastCheckpoint.GroupId > 0)
            {
                lava.SaveProgressAtCheckpoint();
                data.lavaSavedTurns = lava.GetSavedTurns();
            }
            else
            {
                data.lavaSavedTurns = 0;
            }

            SaveSystem.SaveGame(data);

            float safeZone = Mathf.Min(
                player1.GetCommittedCheckpoint().LavaSafeHeight,
                player2.GetCommittedCheckpoint().LavaSafeHeight
            );
            lava.SetSafeZone(safeZone);
        }
    }

    // Métodos para controlar o painel de confirmação na vitória

    public void OpenVictoryConfirmation(System.Action action)
    {
        if (confirmationUI != null)
        {
            confirmationUI.SetActive(true);
            confirmedAction = action;
        }
    }

    public void ConfirmVictoryAction()
    {
        if (confirmationUI != null)
            confirmationUI.SetActive(false);

        confirmedAction?.Invoke();
    }

    public void CancelVictoryAction()
    {
        if (confirmationUI != null)
            confirmationUI.SetActive(false);

        confirmedAction = null;
    }
}
