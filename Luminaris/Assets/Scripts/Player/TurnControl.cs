using UnityEngine;
using System;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    [SerializeField] private PlayerMovement player1;
    [SerializeField] private PlayerMovement player2;

    private PlayerMovement currentPlayer;

    // Referência para a Lava
    private LavaRise lava;

    // Evento global para sinalizar quando um turno acaba
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
        lava = GameObject.FindWithTag("Lava")?.GetComponent<LavaRise>();
        // Começa sempre com o Player1 ativo
        ResetTurns();
    }

    // Reseta estado do sistema de turnos
    public void ResetTurns()
    {
        currentPlayer = player1;
        player1.StartTurn();
        player2.EndTurn();

        lava?.ResetLava();
    }

    // Chamado quando jogador termina seus 3 pulos + aterrissagem
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

        
        lava?.ConsumeTurn();

        OnTurnEnded?.Invoke();
    }
}
