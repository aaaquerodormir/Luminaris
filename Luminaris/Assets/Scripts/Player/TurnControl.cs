using UnityEngine;
using System;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    [SerializeField] private PlayerMovement player1;
    [SerializeField] private PlayerMovement player2;

    private PlayerMovement currentPlayer;

    // Evento global para sinalizar fim de turno
    public static event Action OnTurnEnded;

    private void Awake()
    {
        // Singleton para acesso fácil
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // Começa sempre com player1
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

        // Dispara evento global
        OnTurnEnded?.Invoke();
    }
}
