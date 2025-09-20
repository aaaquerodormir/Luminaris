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
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;

    private bool isGameOver = false;
    public bool IsGameOverActive => isGameOver;

    private GameSession session;
    private Checkpoint lastCheckpoint;

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
        Debug.Log("Turno finalizado!");
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
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (gameOverUI != null) gameOverUI.SetActive(false);

        if (victoryUI != null)
        {
            victoryUI.SetActive(true);

            // Garante que a animação continue tocando mesmo com timeScale = 0
            Animator anim = victoryUI.GetComponent<Animator>();
            if (anim != null)
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        }

        Time.timeScale = 0f;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
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
}
