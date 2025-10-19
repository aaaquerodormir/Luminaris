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

        Debug.Log($"[TurnControl] {players.Count} jogadores encontrados. Ordenando e iniciando turnos...");

        // 🔹 Garante ordem fixa de OwnerClientId (Player1 = host)
        players = players.OrderBy(p => p.OwnerClientId).ToList();

        foreach (var p in players)
            Debug.Log($"[TurnControl] Registrado: {p.name} (Owner={p.OwnerClientId})");

        ResetTurns();
    }

    private void FindPlayersInScene()
    {
        var found = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var p in found)
        {
            if (!players.Contains(p))
                players.Add(p);
        }
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (!IsServer || player == null) return;

        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[TurnControl] Player registrado: {player.name} (Owner={player.OwnerClientId})");
        }

        if (players.Count >= 2)
        {
            players = players.OrderBy(p => p.OwnerClientId).ToList();
            Debug.Log("[TurnControl] Dois jogadores registrados — iniciando turnos ordenados.");
            ResetTurns();
        }
    }

    public void ResetTurns()
    {
        if (!IsServer) return;

        foreach (var p in players)
            if (p != null)
                p.SetTurnActiveServerRpc(false);

        if (players.Count == 0)
        {
            Debug.LogError("[TurnControl] Nenhum player registrado!");
            return;
        }

        currentIndex.Value = 0;
        TriggerTurnStarted(players[currentIndex.Value]);
    }

    public void EndTurn()
    {
        if (!IsServer || players.Count == 0) return;

        var current = players[currentIndex.Value];
        Debug.Log($"[TurnControl] Encerrando turno de {current.name} (Index={currentIndex.Value})");
        current?.SetTurnActiveServerRpc(false);

        currentIndex.Value = (currentIndex.Value + 1) % players.Count;
        Debug.Log($"[TurnControl] Passando turno para {players[currentIndex.Value].name} (Index={currentIndex.Value})");

        TriggerTurnStarted(players[currentIndex.Value]);
    }

    private void TriggerTurnStarted(PlayerMovement player)
    {
        if (player == null) return;

        Debug.Log($"[TurnControl] 🔹 Novo turno iniciado — Player ativo: {player.name} (Owner={player.OwnerClientId})");
        player.SetTurnActiveServerRpc(true);
        OnTurnStarted?.Invoke(player);
    }
}
