using UnityEngine;
using System;
using System.Collections.Generic;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    //[SerializeField] private PlayerMovement player1;
    //[SerializeField] private PlayerMovement player2;
   
    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    private int currentIndex = 0;
    private PlayerMovement CurrentPlayer => players[currentIndex];                                          

    //private PlayerMovement currentPlayer;

    // Referência para a Lava
    [Header("Referências")]
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
        //currentPlayer = player1;
        //player1.StartTurn();
        //player2.EndTurn();

        //lava?.ResetLava();

        foreach (var p in players)
            p.EndTurn();

        currentIndex = 0;
        players[currentIndex].StartTurn();

        lava?.ResetLava();
    }

    // Chamado quando jogador termina seus 3 pulos + aterrissagem
    public void EndTurnIfReady()
    {
        //if (currentPlayer == player1)
        //{
        //    player1.EndTurn();
        //    currentPlayer = player2;
        //    player2.StartTurn();
        //}
        //else
        //{
        //    player2.EndTurn();
        //    currentPlayer = player1;
        //    player1.StartTurn();
        //}

        players[currentIndex].EndTurn();

        // Avança para o próximo jogador (loop infinito)
        currentIndex = (currentIndex + 1) % players.Count;

        players[currentIndex].StartTurn();


        lava?.ConsumeTurn();

        OnTurnEnded?.Invoke();
    }
}
