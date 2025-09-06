using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerMovementUI : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private int baseMaxJumps = 3; // Pulos base por turno

    private int jumpsUsed = 0;
    private readonly List<(int extraJumps, int turnsLeft)> activeJumpPowerUps = new();

    public event Action OnJumpsChanged;

    // Quantidade máxima de pulos neste turno (base + extras válidos)
    public int MaxJumps
    {
        get
        {
            int extras = 0;
            foreach (var power in activeJumpPowerUps)
                extras += power.extraJumps;

            return baseMaxJumps + extras;
        }
    }

    public int JumpsUsed => jumpsUsed;
    public int RemainingJumps => MaxJumps - jumpsUsed;

    public void ConsumeJump()
    {
        if (jumpsUsed >= MaxJumps) return;

        jumpsUsed++;
        Debug.Log($"[DEBUG] {gameObject.name} consumiu 1 pulo. Restando: {RemainingJumps} ({jumpsUsed}/{MaxJumps})");

        OnJumpsChanged?.Invoke();
    }

    public void StartTurn()
    {
        jumpsUsed = 0;

        // Reduz duração dos powerups
        for (int i = activeJumpPowerUps.Count - 1; i >= 0; i--)
        {
            var p = activeJumpPowerUps[i];
            p.turnsLeft--;
            if (p.turnsLeft <= 0)
                activeJumpPowerUps.RemoveAt(i);
            else
                activeJumpPowerUps[i] = p;
        }

        Debug.Log($"[DEBUG] {gameObject.name} iniciou turno: Base={baseMaxJumps}, Extras={GetTotalExtraJumps()}, Max={MaxJumps}");
        OnJumpsChanged?.Invoke();
    }

    public void EndTurn()
    {
        Debug.Log($"[DEBUG] {gameObject.name} terminou turno ({jumpsUsed}/{MaxJumps} usados).");
        OnJumpsChanged?.Invoke();
    }

    public void AddJumpPowerUp(int extraJumps, int duration)
    {
        if (extraJumps <= 0) return;

        activeJumpPowerUps.Add((extraJumps, duration));

        Debug.Log($"[DEBUG] {gameObject.name} ganhou PowerUp: +{extraJumps} pulos por {duration} turnos. " +
                  $"Base={baseMaxJumps}, Extras={GetTotalExtraJumps()}, Max={MaxJumps}");

        OnJumpsChanged?.Invoke();
    }

    private int GetTotalExtraJumps()
    {
        int total = 0;
        foreach (var p in activeJumpPowerUps)
            total += p.extraJumps;
        return total;
    }
}
