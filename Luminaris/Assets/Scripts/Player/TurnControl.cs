using UnityEngine;
using System;
using System.Collections.Generic;

public class TurnControl : MonoBehaviour
{
    public static TurnControl Instance;

    [Header("Jogadores")]
    [SerializeField] private List<PlayerMovement> players = new();

    private int currentIndex = 0;
    private PlayerMovement CurrentPlayer => players.Count > 0 ? players[currentIndex] : null;

    [Header("Referências")]
    [SerializeField] private LavaRise lava;

    public static event Action OnTurnEnded;

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

    private void EnsureLavaReference()
    {
        if (lava != null) return;

        // 1) Procurar por nome
        var byName = GameObject.Find("Lava");
        if (byName != null)
        {
            lava = byName.GetComponent<LavaRise>();
            if (lava != null)
            {
                return;
            }
        }

        // 2) Procurar por tag
        var byTag = GameObject.FindWithTag("Lava");
        if (byTag != null)
        {
            lava = byTag.GetComponent<LavaRise>();
            if (lava != null)
            {
                return;
            }
        }

        // 3) Unity 6: procurar o primeiro objeto do tipo
        lava = UnityEngine.Object.FindFirstObjectByType<LavaRise>();
        if (lava != null)
        {
            return;
        }

        // 4) fallback: qualquer objeto do tipo
        lava = UnityEngine.Object.FindAnyObjectByType<LavaRise>();
        if (lava != null)
        {
            return;
        }

    }

    public void ResetTurns()
    {
        foreach (var p in players)
            p.EndTurn();

        currentIndex = 0;
        if (players.Count > 0)
            players[currentIndex].StartTurn();
    }

    public void EndTurnIfReady()
    {
        if (players.Count == 0) return;

        players[currentIndex].EndTurn();

        currentIndex = (currentIndex + 1) % players.Count;
        players[currentIndex].StartTurn();


        if (lava == null)
            EnsureLavaReference();

        OnTurnEnded?.Invoke();
    }
}
