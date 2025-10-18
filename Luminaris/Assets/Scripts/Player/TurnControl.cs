using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class TurnControl : NetworkBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores Registrados")]
    public List<PlayerMovement> players = new();

    private NetworkVariable<int> currentIndex = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
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
            Debug.Log("[TurnControl] Servidor aguardando jogadores...");
            StartCoroutine(WaitForPlayersAndStartTurns());
        }
    }

    private IEnumerator WaitForPlayersAndStartTurns()
    {
        yield return new WaitForSeconds(1f);

        while (players.Count < 2)
        {
            FindPlayersInScene();
            Debug.Log($"[TurnControl] Encontrados {players.Count}/2 jogadores...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log($"[TurnControl] {players.Count} jogadores detectados. Iniciando sistema de turnos.");
        ResetTurns();
    }

    private void FindPlayersInScene()
    {
        var found = FindObjectsOfType<PlayerMovement>();
        foreach (var p in found)
            if (!players.Contains(p)) players.Add(p);
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (!IsServer || player == null) return;

        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[TurnControl] Player registrado: {player.name}");
        }

        if (players.Count >= 2 && IsServer && currentIndex.Value == 0)
        {
            Debug.Log("[TurnControl] Dois jogadores registrados — iniciando turnos.");
            ResetTurns();
        }
    }

    public void ResetTurns()
    {
        if (!IsServer) return;

        Debug.Log("[TurnControl] Reiniciando turnos...");
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
        current?.SetTurnActiveServerRpc(false);

        currentIndex.Value = (currentIndex.Value + 1) % players.Count;
        TriggerTurnStarted(players[currentIndex.Value]);
    }

    private void TriggerTurnStarted(PlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogWarning("[TurnControl] Tentou iniciar turno com Player nulo!");
            return;
        }

        Debug.Log($"[TurnControl] Novo turno: {player.name}");
        player.SetTurnActiveServerRpc(true);
        OnTurnStarted?.Invoke(player);
    }
}
