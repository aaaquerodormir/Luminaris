using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class TurnControl : NetworkBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    private NetworkVariable<int> currentIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static event Action<PlayerMovement> OnTurnStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (IsServer)
        {
            Debug.Log("[TurnControl] Servidor inicializando turnos...");
            ResetTurns();
        }

        currentIndex.OnValueChanged += (oldVal, newVal) =>
        {
            Debug.Log($"[TurnControl] currentIndex alterado {oldVal} → {newVal}");
        };
    }

    public void ResetTurns()
    {
        if (!IsServer) return;
        Debug.Log("[TurnControl] Resetando turnos...");

        foreach (var p in players)
        {
            if (p == null) continue;
            p.EndTurn();
        }

        currentIndex.Value = 0;

        if (players.Count > 0)
        {
            players[currentIndex.Value].StartTurn();
            Debug.Log($"[TurnControl] Primeiro turno: {players[currentIndex.Value].name}");
            OnTurnStarted?.Invoke(players[currentIndex.Value]);
        }
    }

    public void EndTurnIfReady()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[TurnControl] Tentativa de encerrar turno por cliente — ignorada.");
            return;
        }

        if (players.Count == 0) return;

        Debug.Log($"[TurnControl] Encerrando turno do jogador {currentIndex.Value}");
        players[currentIndex.Value].EndTurn();

        currentIndex.Value = (currentIndex.Value + 1) % players.Count;

        Debug.Log($"[TurnControl] Próximo turno: {players[currentIndex.Value].name}");
        players[currentIndex.Value].StartTurn();
        OnTurnStarted?.Invoke(players[currentIndex.Value]);
    }
}
