using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class TurnControl : NetworkBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    [SerializeField] private LavaRise lava;

    public static event Action<PlayerMovement> OnTurnStarted;
    private int currentIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (IsServer)
        {
            Debug.Log("[TurnControl] Servidor inicializando turnos.");
            ResetTurns();
        }
    }

    public void ResetTurns()
    {
        if (!IsServer) return;

        foreach (var p in players)
            p.EndTurn();

        currentIndex = 0;
        players[currentIndex].StartTurn();
        Debug.Log($"[TurnControl] Turno iniciado com {players[currentIndex].name}");

        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void EndTurnIfReady()
    {
        if (!IsServer) return;

        players[currentIndex].EndTurn();
        currentIndex = (currentIndex + 1) % players.Count;
        players[currentIndex].StartTurn();

        Debug.Log($"[TurnControl] Turno trocado: {players[currentIndex].name}");
        OnTurnStarted?.Invoke(players[currentIndex]);
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }
}
