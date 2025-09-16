using UnityEngine;

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
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;

    private bool isGameOver = false;
    public bool IsGameOverActive => isGameOver;

    private GameSession session;
    private Checkpoint lastCheckpoint;

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

    // Salva somente quando ambos alcançam o mesmo GroupId
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

            // lava só guarda progresso se for group > 0
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

            Debug.Log("Progresso salvo no grupo " + lastCheckpoint.GroupId);
        }
        else
        {
            Debug.Log("Checkpoint aguardando sincronização (grupo " + cp.GroupId + ")");
        }
    }
}
