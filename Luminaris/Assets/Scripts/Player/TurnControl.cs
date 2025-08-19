using UnityEngine;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    public PlayerMovement player1;
    public PlayerMovement player2;

    private PlayerMovement currentPlayer;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        currentPlayer = player1;
        player1.StartTurn();
        player2.EndTurn();
    }

    // Chamado quando um jogador terminou seus 3 pulos e aterrissou
    public void EndTurnIfReady()
    {
        if (currentPlayer == player1)
        {
            player1.EndTurn();
            currentPlayer = player2;
            player2.StartTurn();
        }
        else
        {
            player2.EndTurn();
            currentPlayer = player1;
            player1.StartTurn();
        }
    }
}
