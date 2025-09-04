using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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

        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(false);

        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
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

        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(true);

        isGameOver = false;
    }

    public void ReachCheckpoint(Transform checkpointTransform)
    {
        var checkpoint = checkpointTransform.GetComponent<Checkpoint>();
        lastCheckpoint = checkpoint;

        SaveData data = new SaveData
        {
            checkpointIndex = checkpoint.Index
        };
        SaveSystem.SaveGame(data);

        lava.SetSafeZone(checkpoint.LavaSafeHeight);
        Debug.Log("Progresso salvo no checkpoint " + checkpoint.Index);
    }
}
