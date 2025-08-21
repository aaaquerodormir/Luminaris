using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Jogadores")]
    [SerializeField] private PlayerRespawn player1;
    [SerializeField] private PlayerRespawn player2;

    [Header("Lava")]
    [SerializeField] private LavaRise lava; // referência para resetar lava

    [Header("Controle de Turnos")]
    [SerializeField] private TurnControl turnControl; // referência para resetar turnos

    [Header("UI")]
    [SerializeField] private GameObject gameOverUI; // painel de Game Over

    private bool isGameOver = false;

    private void Awake()
    {
        // Singleton para garantir que só exista 1 GameManager
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Escuta eventos de morte de jogador e fim de turno
        PlayerRespawn.OnPlayerDied += ShowGameOver;
        TurnControl.OnTurnEnded += HandleTurnEnd;
    }

    private void OnDisable()
    {
        // Remove inscrições de eventos
        PlayerRespawn.OnPlayerDied -= ShowGameOver;
        TurnControl.OnTurnEnded -= HandleTurnEnd;
    }

    private void HandleTurnEnd()
    {
        // Aqui pode ser adicionada lógica extra (ex: acelerar lava com o tempo)
        Debug.Log("Turno finalizado!");
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Ativa tela de Game Over e pausa o jogo
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TentarNovamente()
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
