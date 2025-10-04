using UnityEngine;
using System;
using System.Collections.Generic;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    // Propriedade pública para que outros scripts possam saber quem está jogando.
    public PlayerMovement CurrentPlayer => players.Count > 0 ? players[currentIndex] : null;
    private int currentIndex = 0;

    [Header("Referências")]
    [SerializeField] private LavaRise lava;

 
    public static event Action<PlayerMovement> OnTurnStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        EnsureLavaReference();
        ResetTurns();
    }

    public void ResetTurns()
    {
        foreach (var p in players)
            p.EndTurn();

        currentIndex = 0;
        if (players.Count > 0)
        {
            players[currentIndex].StartTurn();
            // Dispara o evento para o primeiro turno do jogo, informando quem começa.
            OnTurnStarted?.Invoke(players[currentIndex]);
        }
    }

    public void EndTurnIfReady()
    {
        if (players.Count == 0) return;

        players[currentIndex].EndTurn();

        currentIndex = (currentIndex + 1) % players.Count;
        players[currentIndex].StartTurn();

        if (lava == null)
            EnsureLavaReference();

        // Dispara o evento, enviando o jogador ATUAL como informação.
        OnTurnStarted?.Invoke(CurrentPlayer);
    }

    private void EnsureLavaReference()
    {
        if (lava != null) return;

        var byName = GameObject.Find("Lava");
        if (byName != null) { lava = byName.GetComponent<LavaRise>(); if (lava != null) return; }

        var byTag = GameObject.FindWithTag("Lava");
        if (byTag != null) { lava = byTag.GetComponent<LavaRise>(); if (lava != null) return; }

        lava = FindFirstObjectByType<LavaRise>();
        if (lava != null) return;

        lava = FindAnyObjectByType<LavaRise>();
    }
}