using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class TurnControl : NetworkBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores Registrados")]
    public List<PlayerMovement> players = new List<PlayerMovement>();

    private NetworkVariable<int> currentIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public static event System.Action<PlayerMovement> OnTurnStarted;

    private bool gameStarted = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (IsServer)
        {
            Debug.Log("[TurnControl] Servidor inicializando controle de turnos...");
            StartCoroutine(WaitForPlayersAndStartTurns());
        }
    }

    private IEnumerator WaitForPlayersAndStartTurns()
    {
        // Espera até que os players existam na cena (spawnados via rede)
        yield return new WaitForSeconds(1f);

        while (players.Count < 2)
        {
            FindPlayersInScene();
            Debug.Log($"[TurnControl] Aguardando jogadores... ({players.Count}/2)");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"[TurnControl] {players.Count} jogadores encontrados. Iniciando turnos...");
        ResetTurns();
        gameStarted = true;
    }

    // ============================================
    // === Procurar jogadores na cena automaticamente
    // ============================================
    private void FindPlayersInScene()
    {
        var foundPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (var p in foundPlayers)
        {
            if (p != null && !players.Contains(p))
            {
                players.Add(p);
                Debug.Log($"[TurnControl] Player encontrado na cena e registrado automaticamente: {p.name}");
            }
        }
    }

    // ============================================
    // === Registro manual via PlayerMovement ======
    // ============================================
    public void RegisterPlayer(PlayerMovement player)
    {
        if (!IsServer) return;
        if (player == null)
        {
            Debug.LogWarning("[TurnControl] Tentou registrar um player nulo!");
            return;
        }

        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[TurnControl] Player registrado: {player.name} | Total: {players.Count}");
        }
        else
        {
            Debug.Log($"[TurnControl] Player {player.name} já estava registrado.");
        }
    }

    // ============================================
    // === Controle de turnos =====================
    // ============================================
    public void ResetTurns()
    {
        if (!IsServer)
        {
            Debug.Log("[TurnControl] Cliente tentou resetar turnos (ignorado).");
            return;
        }

        Debug.Log("[TurnControl] Resetando turnos...");

        foreach (var p in players)
        {
            if (p != null)
                p.SetTurnActiveServerRpc(false);
        }

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
        if (!IsServer) return;

        if (players.Count == 0)
        {
            Debug.LogError("[TurnControl] Nenhum player para trocar turno!");
            return;
        }

        players[currentIndex.Value].SetTurnActiveServerRpc(false);
        currentIndex.Value = (currentIndex.Value + 1) % players.Count;
        TriggerTurnStarted(players[currentIndex.Value]);
    }

    private void TriggerTurnStarted(PlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogWarning("[TurnControl] Tentou disparar OnTurnStarted, mas o player é nulo!");
            return;
        }

        Debug.Log($"[TurnControl] 🔁 Novo turno iniciado — Jogador ativo: {player.name}");
        player.SetTurnActiveServerRpc(true);
        OnTurnStarted?.Invoke(player);
    }
}
