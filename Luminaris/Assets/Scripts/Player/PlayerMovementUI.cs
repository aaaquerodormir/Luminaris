using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerMovementUI : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private int baseMaxJumps = 3; // pulos base por turno

    private int jumpsUsed = 0; // quantos pulos ja foram consumidos neste turno
    private List<(int extraJumps, int turnsLeft)> activeJumpPowerUps = new();

    // Evento para HUD
    public event Action OnJumpsChanged;

    // Quantidade maxima de pulos neste turno (base + powerups ativos)
    public int MaxJumps
    {
        get
        {
            int total = baseMaxJumps;
            foreach (var power in activeJumpPowerUps)
                total += power.extraJumps;
            return total;
        }
    }

    // Quantos pulos ja foram usados neste turno
    public int JumpsUsed => jumpsUsed;

    // Quantos pulos ainda restam neste turno
    public int RemainingJumps => MaxJumps - jumpsUsed;

    // Consome 1 pulo
    public void ConsumeJump()
    {
        if (jumpsUsed >= MaxJumps) return;

        jumpsUsed++;
        Debug.Log($"[DEBUG] {gameObject.name} consumiu 1 pulo. Restando: {RemainingJumps} ({jumpsUsed}/{MaxJumps})");

        OnJumpsChanged?.Invoke();
    }

    // Chamado no inicio do turno
    public void StartTurn()
    {
        jumpsUsed = 0;

        // Reduz duracao dos powerups
        for (int i = activeJumpPowerUps.Count - 1; i >= 0; i--)
        {
            var p = activeJumpPowerUps[i];
            p.turnsLeft--;
            if (p.turnsLeft <= 0)
                activeJumpPowerUps.RemoveAt(i);
            else
                activeJumpPowerUps[i] = p;
        }

        Debug.Log($"[DEBUG] {gameObject.name} iniciou turno com {MaxJumps} pulos.");
        OnJumpsChanged?.Invoke();
    }

    // Chamado no fim do turno
    public void EndTurn()
    {
        Debug.Log($"[DEBUG] {gameObject.name} terminou turno ({jumpsUsed}/{MaxJumps} usados).");
        OnJumpsChanged?.Invoke();
    }

    // Ganha powerup de pulos extras
    public void AddJumpPowerUp(int extraJumps, int duration)
    {
        activeJumpPowerUps.Add((extraJumps, duration));
        Debug.Log($"[DEBUG] {gameObject.name} ganhou PowerUp: +{extraJumps} pulos por {duration} turnos. Agora MaxJumps = {MaxJumps}");

        OnJumpsChanged?.Invoke();
    }
}
