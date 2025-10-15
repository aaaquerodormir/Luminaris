using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    public int RemainingJumps => jumpsNetworked.Value;

    private NetworkVariable<int> jumpsNetworked = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public static event System.Action<PlayerMovementUI, int> OnJumpsChanged;

    public void StartTurn(int maxJumps)
    {
        if (IsServer)
            jumpsNetworked.Value = maxJumps;

        Debug.Log($"[PlayerMovementUI] {name} iniciou turno. Máx = {maxJumps}");
        OnJumpsChanged?.Invoke(this, maxJumps);
    }

    public void UpdateJumps(int newValue)
    {
        if (IsServer)
            jumpsNetworked.Value = newValue;

        OnJumpsChanged?.Invoke(this, newValue);
    }

    public void EndTurn()
    {
        Debug.Log($"[PlayerMovementUI] {name} terminou turno ({jumpsNetworked.Value}/3)");
        OnJumpsChanged?.Invoke(this, jumpsNetworked.Value);
    }

    private void OnEnable()
    {
        jumpsNetworked.OnValueChanged += (_, newVal) =>
        {
            OnJumpsChanged?.Invoke(this, newVal);
        };
    }

    private void OnDisable()
    {
        jumpsNetworked.OnValueChanged -= (_, __) => { };
    }
}
