using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    private NetworkVariable<int> jumpsNetworked = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public int RemainingJumps => jumpsNetworked.Value;
    public static event System.Action<PlayerMovementUI, int> OnJumpsChanged;

    public void StartTurn(int maxJumps)
    {
        if (IsServer) jumpsNetworked.Value = maxJumps;
        else UpdateJumpsServerRpc(maxJumps);

        OnJumpsChanged?.Invoke(this, maxJumps);
    }

    public void UpdateJumps(int newValue)
    {
        if (IsServer) jumpsNetworked.Value = newValue;
        else UpdateJumpsServerRpc(newValue);

        OnJumpsChanged?.Invoke(this, newValue);
    }

    public void EndTurn()
    {
        OnJumpsChanged?.Invoke(this, jumpsNetworked.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateJumpsServerRpc(int newValue, ServerRpcParams rpcParams = default)
    {
        jumpsNetworked.Value = newValue;
        OnJumpsChanged?.Invoke(this, newValue);
    }

    private void OnEnable()
    {
        jumpsNetworked.OnValueChanged += HandleJumpChanged;
    }

    private void OnDisable()
    {
        jumpsNetworked.OnValueChanged -= HandleJumpChanged;
    }

    private void HandleJumpChanged(int oldVal, int newVal)
    {
        Debug.Log($"[SYNC:{name}] Jumps {oldVal} → {newVal}");
        OnJumpsChanged?.Invoke(this, newVal);
    }
}

