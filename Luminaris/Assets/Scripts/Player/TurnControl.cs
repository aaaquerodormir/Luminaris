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

    [Header("UI de Turno")]
    [Tooltip("O GameObject do 'PainelDeTurnoLuna'")]
    [SerializeField] private GameObject uiPlayer1Turn; // Ligado ao PainelDeTurnoLuna
    [Tooltip("O GameObject do 'PainelDeTurnoLuma'")]
    [SerializeField] private GameObject uiPlayer2Turn; // Ligado ao PainelDeTurnoLuma

    // --- 1. ADICIONADO (MOVIDO DO GAMEMANAGER) ---
    [Tooltip("Quantos segundos o painel de troca de turno fica na tela.")]
    [SerializeField] private float turnPanelDisplayDuration = 2.5f;
    // ---------------------------------------------

    public static event Action<PlayerMovement> OnTurnStarted;

    // Armazena a corrotina do pop-up para podermos pará-la
    private Coroutine turnPanelCoroutine;

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
        // --- 2. LÓGICA DE UI E DURAÇÃO ATUALIZADA ---

        if (uiPlayer1Turn == null || uiPlayer2Turn == null)
        {
            Debug.LogWarning("[TurnControl] UIs de turno (P1 ou P2) não atribuídas no Inspector!");
            return;
        }

        // Para qualquer corrotina que esteja rodando para evitar que a UI fique presa
        if (turnPanelCoroutine != null)
        {
            StopCoroutine(turnPanelCoroutine);
            // Garante que o painel anterior seja desativado
            uiPlayer1Turn.SetActive(false);
            uiPlayer2Turn.SetActive(false);
        }

        GameObject panelToShow = null;

        if (playerName.Contains("Player1", StringComparison.OrdinalIgnoreCase))
        {
            panelToShow = uiPlayer1Turn;
        }
        else if (playerName.Contains("Player2", StringComparison.OrdinalIgnoreCase))
        {
            panelToShow = uiPlayer2Turn;
        }

        // Inicia a nova corrotina para mostrar o painel correto
        if (panelToShow != null)
        {
            turnPanelCoroutine = StartCoroutine(ShowTurnPanelRoutine(panelToShow));
            Debug.Log($"[TurnControl] UI atualizada para {playerName} em todos os clientes.");
        }
    }

    // --- 3. NOVA CORROTINA DE DURAÇÃO ---
    private IEnumerator ShowTurnPanelRoutine(GameObject panelToShow)
    {
        panelToShow.SetActive(true);
        // Espera o tempo definido (usando Realtime para funcionar mesmo se o jogo pausar)
        yield return new WaitForSecondsRealtime(turnPanelDisplayDuration);
        panelToShow.SetActive(false);
        turnPanelCoroutine = null; // Limpa a referência
    }

    // --- 4. NOVO MÉTODO PÚBLICO DE LIMPEZA ---
    /// <summary>
    /// Esconde toda a UI de turno. Chamado pelo GameManager em GameOver/Victory.
    /// </summary>
    public void HideAllTurnUI()
    {
        if (turnPanelCoroutine != null)
        {
            StopCoroutine(turnPanelCoroutine);
            turnPanelCoroutine = null;
        }
        if (uiPlayer1Turn != null) uiPlayer1Turn.SetActive(false);
        if (uiPlayer2Turn != null) uiPlayer2Turn.SetActive(false);
    }
}