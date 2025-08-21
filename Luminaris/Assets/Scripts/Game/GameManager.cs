using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Jogadores")]
    [SerializeField] private PlayerRespawn player1;
    [SerializeField] private PlayerRespawn player2;

    [Header("UI")]
    [SerializeField] private GameObject gameOverUI;

    private bool isGameOver = false;

    private void Awake()
    {
        // Garante que só exista um GameManager (Singleton)
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
        // Aqui pode ser usada lógica extra (ex: acelerar lava, somar pontos, etc.)
        Debug.Log("Turno finalizado!");
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // Mostra tela de Game Over e pausa o jogo
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TentarNovamente()
    {
        // Fecha tela de Game Over e retoma tempo
        gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        // Restaura jogadores
        player1.Respawn();
        player2.Respawn();

        isGameOver = false;
    }
}
