using UnityEngine;
using System;
using System.Collections.Generic;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    private int currentIndex = 0;
    private PlayerMovement CurrentPlayer => players[currentIndex];

    [Header("Referências")]
    private LavaRise lava;

    public static event Action OnTurnEnded;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        lava = GameObject.FindWithTag("Lava")?.GetComponent<LavaRise>();
        ResetTurns();
    }

    public void ResetTurns()
    {
        foreach (var p in players)
            p.EndTurn();

        currentIndex = 0;
        players[currentIndex].StartTurn();

        // lava NÃO reseta mais aqui
    }

    public void EndTurnIfReady()
    {
        players[currentIndex].EndTurn();

        currentIndex = (currentIndex + 1) % players.Count;
        players[currentIndex].StartTurn();

        lava?.ConsumeTurn();

        OnTurnEnded?.Invoke();
    }
}
