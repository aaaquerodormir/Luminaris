using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public class TurnControl : NetworkBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores Registrados")]
    public List<PlayerMovement> players = new();

    private NetworkVariable<int> currentIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public static event Action<PlayerMovement> OnTurnStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (IsServer)
            StartCoroutine(WaitForPlayersAndStartTurns());
    }

    private IEnumerator WaitForPlayersAndStartTurns()
    {
        yield return new WaitForSeconds(1f);
        while (players.Count < 2)
        {
            FindPlayersInScene();
            yield return new WaitForSeconds(0.5f);
        }

        players = players.OrderBy(p => p.OwnerClientId).ToList();
        Debug.Log($"[TurnControl] 🟢 {players.Count} jogadores detectados. Iniciando sequência.");

        ResetTurns();
    }

    private void FindPlayersInScene()
    {
        var found = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in found)
            if (!players.Contains(p)) players.Add(p);
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (!IsServer || player == null || players.Contains(player)) return;

        players.Add(player);
        players = players.OrderBy(p => p.OwnerClientId).ToList();
        Debug.Log($"[TurnControl] ➕ Registrado: {player.name} (Owner={player.OwnerClientId})");

        if (players.Count >= 2)
            ResetTurns();
    }

    public void ResetTurns()
    {
        if (!IsServer) return;

        foreach (var p in players)
            p?.SetTurnActiveServerRpc(false);

        currentIndex.Value = 0;
        TriggerTurnStarted(players[currentIndex.Value]);
    }

    public void EndTurn()
    {
        if (!IsServer || players.Count == 0) return;

        var current = players[currentIndex.Value];
        current?.SetTurnActiveServerRpc(false);

        // NOVO: decrementar buffs do jogador cujo turno acabou
        current?.OnTurnEndedServer();

        currentIndex.Value = (currentIndex.Value + 1) % players.Count;
        Debug.Log($"[TurnControl] 🔁 Passando turno -> {players[currentIndex.Value].name}");
        TriggerTurnStarted(players[currentIndex.Value]);
    }

    private void TriggerTurnStarted(PlayerMovement player)
    {
        if (player == null) return;

        if (IsServer)
        {
            player.CompletedJumpsNet.Value = 0;
            Debug.Log($"[TurnControl] Jumps Resetados para {player.name}.");
        }

        Debug.Log($"[TurnControl] ▶ Turno ativo: {player.name}");
        player.SetTurnActiveServerRpc(true);
        OnTurnStarted?.Invoke(player);
    }
}