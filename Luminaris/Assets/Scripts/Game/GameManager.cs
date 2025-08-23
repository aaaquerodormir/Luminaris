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
    [SerializeField] private PauseMenu pauseMenu; // referência direta ao pause

    private bool isGameOver = false;
    public bool IsGameOverActive => isGameOver;

    private void Awake()
    {
        // Singleton para garantir que só exista 1 GameManager
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Escuta eventos
        PlayerRespawn.OnPlayerDied += ShowGameOver;
        TurnControl.OnTurnEnded += HandleTurnEnd;
    }

    private void OnDisable()
    {
        // Remove eventos
        PlayerRespawn.OnPlayerDied -= ShowGameOver;
        TurnControl.OnTurnEnded -= HandleTurnEnd;
    }

    private void HandleTurnEnd()
    {
        Debug.Log("Turno finalizado!");
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Força desligar o pause inteiro
        if (pauseMenu != null)
            pauseMenu.gameObject.SetActive(false);

        // Ativa tela de Game Over e pausa o jogo
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TryAgain()
    {
        // Fecha tela de Game Over e retoma tempo
        gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        // Reseta jogadores
        player1.Respawn();
        player2.Respawn();

        // Reseta lava
        lava.ResetLava();

        // Reseta turnos
        turnControl.ResetTurns();

        isGameOver = false;
    }
}
