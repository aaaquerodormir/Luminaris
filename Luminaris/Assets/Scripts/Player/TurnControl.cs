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

    [Header("Referências da Cena")]
    [SerializeField] private LavaRise lavaInstance;

    // --- ALTERAÇÃO 1: Renomeado para clareza ---
    [Header("UI de Turno")]
    [SerializeField] private GameObject uiPlayer1Turn; // Anteriormente uiLunaTurn
    [SerializeField] private GameObject uiPlayer2Turn; // Anteriormente uiLumaTurn
                                                       // -------------------------------------------

    public static event Action<PlayerMovement> OnTurnStarted;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (IsServer && lavaInstance == null)
        {
            lavaInstance = FindFirstObjectByType<LavaRise>();
            if (lavaInstance != null)
                Debug.Log("[TurnControl] Referência da Lava encontrada na cena.");
            else
                Debug.LogError("[TurnControl] NÃO FOI POSSÍVEL ENCONTRAR LavaRise NA CENA!");
        }
    }

    private void Start()
    {
        if (IsServer)
            StartCoroutine(WaitForPlayersAndStartTurns());
    }

    private IEnumerator WaitForPlayersAndStartTurns()
    {
        // Espera pelos jogadores (o Spawner agora registra eles)
        // Mas mantém o FindPlayersInScene como um fallback
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
        {
            if (!players.Contains(p))
                players.Add(p);
        }
    }

    public void RegisterPlayer(PlayerMovement player)
    {
        if (!IsServer || player == null || players.Contains(player)) return;

        players.Add(player);
        players = players.OrderBy(p => p.OwnerClientId).ToList();
        Debug.Log($"[TurnControl] ➕ Registrado: {player.name} (Owner={player.OwnerClientId})");

        // Inicia os turnos assim que o segundo jogador for registrado
        if (players.Count == 2)
            ResetTurns();
    }

    public PlayerMovement GetCurrentActivePlayer()
    {
        if (players.Count == 0 || currentIndex.Value < 0 || currentIndex.Value >= players.Count)
            return null;

        return players[currentIndex.Value];
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

        current?.DecrementBuffTurns();
        lavaInstance?.DecrementBuffTurns();

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

        if (IsServer)
            UpdateTurnUIClientRpc(player.name);
    }


    // ================================================================
    // =====================  UI DE TURNO  =============================
    // ================================================================

    [ClientRpc]
    private void UpdateTurnUIClientRpc(string playerName)
    {
        // --- ALTERAÇÃO 2: Lógica da UI atualizada ---
        // Usa as novas variáveis
        if (uiPlayer1Turn == null || uiPlayer2Turn == null)
        {
            Debug.LogWarning("[TurnControl] UIs de turno (P1 ou P2) não atribuídas no Inspector!");
            return;
        }

        uiPlayer1Turn.SetActive(false);
        uiPlayer2Turn.SetActive(false);

        // Checa por "Player1" ou "Player2" no nome do objeto do jogador
        // (O Spawner usa player1Prefab e player2Prefab, então o nome instanciado conterá isso)
        if (playerName.Contains("Player1", StringComparison.OrdinalIgnoreCase))
        {
            uiPlayer1Turn.SetActive(true);
        }
        else if (playerName.Contains("Player2", StringComparison.OrdinalIgnoreCase))
        {
            uiPlayer2Turn.SetActive(true);
        }
        // --- FIM DAS ALTERAÇÕES ---

        Debug.Log($"[TurnControl] UI atualizada para {playerName}");
    }
}