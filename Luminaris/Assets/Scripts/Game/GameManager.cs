using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Jogadores")]
    public PlayerRespawn player1;
    public PlayerRespawn player2;

    [Header("UI")]
    public GameObject gameOverUI;

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += ShowGameOver;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= ShowGameOver;
    }

    public void ShowGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        gameOverUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TentarNovamente()
    {
        gameOverUI.SetActive(false);
        Time.timeScale = 1f;

        player1.Respawn();
        player2.Respawn();

        isGameOver = false;
    }
}
