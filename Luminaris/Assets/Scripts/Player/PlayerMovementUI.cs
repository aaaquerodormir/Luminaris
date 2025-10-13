using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    public int RemainingJumps { get; private set; }

    public delegate void JumpUpdate(PlayerMovementUI player, int jumps);
    public static event JumpUpdate OnJumpsChanged;

    public void StartTurn(int maxJumps)
    {
        RemainingJumps = maxJumps;
        Debug.Log($"[PlayerMovementUI] {name} iniciou turno. Máx = {maxJumps}");
        OnJumpsChanged?.Invoke(this, RemainingJumps);
    }

    public void UpdateJumps(int newValue)
    {
        RemainingJumps = newValue;
        OnJumpsChanged?.Invoke(this, RemainingJumps);
    }

    public void EndTurn()
    {
        Debug.Log($"[PlayerMovementUI] {name} terminou turno ({RemainingJumps}/3)");
        OnJumpsChanged?.Invoke(this, RemainingJumps);
    }
}
