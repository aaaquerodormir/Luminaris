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
    [SerializeField] private GameObject hudContainer; // <- HUD de pulos (HUDContainer)

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

        if (hudContainer != null)
            hudContainer.SetActive(false); // esconde HUD de pulo

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

        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(true);

        if (hudContainer != null)
            hudContainer.SetActive(true); // mostra HUD de pulo de novo

        isGameOver = false;

        OnTryAgain?.Invoke();
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
