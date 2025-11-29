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
    [SerializeField] private GameObject uiPlayer1Turn;
    [Tooltip("O GameObject do 'PainelDeTurnoLuma'")]
    [SerializeField] private GameObject uiPlayer2Turn;

    [Tooltip("Quantos segundos o painel de troca de turno fica na tela.")]
    [SerializeField] private float turnPanelDisplayDuration = 2.5f;

    public static event Action<PlayerMovement> OnTurnStarted;

    private Coroutine turnPanelCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (IsServer && lavaInstance == null)
        {
            lavaInstance = FindFirstObjectByType<LavaRise>();
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

        TriggerTurnStarted(players[currentIndex.Value]);
    }

    private void TriggerTurnStarted(PlayerMovement player)
    {
        if (player == null) return;

        if (IsServer)
        {
            player.CompletedJumpsNet.Value = 0;
        }
        player.SetTurnActiveServerRpc(true);
        OnTurnStarted?.Invoke(player);

        if (IsServer)
            UpdateTurnUIClientRpc(player.name);
    }

    [ClientRpc]
    private void UpdateTurnUIClientRpc(string playerName)
    {
        if (uiPlayer1Turn == null || uiPlayer2Turn == null)
        {
            return;
        }
        if (turnPanelCoroutine != null)
        {
            StopCoroutine(turnPanelCoroutine);
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
        if (panelToShow != null)
        {
            turnPanelCoroutine = StartCoroutine(ShowTurnPanelRoutine(panelToShow));
        }
    }

    private IEnumerator ShowTurnPanelRoutine(GameObject panelToShow)
    {
        panelToShow.SetActive(true);
        yield return new WaitForSecondsRealtime(turnPanelDisplayDuration);
        panelToShow.SetActive(false);
        turnPanelCoroutine = null; 
    }
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