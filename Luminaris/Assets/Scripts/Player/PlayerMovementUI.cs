using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private int baseMaxJumps = 3; // Pulos base por turno


    private NetworkVariable<int> jumpsUsed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> extraJumps = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action OnJumpsChanged;

    public int MaxJumps => baseMaxJumps + extraJumps.Value;
    public int JumpsUsed => jumpsUsed.Value;
    public int RemainingJumps => MaxJumps - JumpsUsed;

    public bool CanJump() => RemainingJumps > 0;

    public void ConsumeJump()
    {
        if (!IsServer) return;
        if (jumpsUsed.Value >= MaxJumps) return;

        jumpsUsed.Value++;
        Debug.Log($"[PlayerMovementUI] {name} consumiu pulo ({RemainingJumps} restantes)");
        OnJumpsChanged?.Invoke();
    }

    public void StartTurn()
    {
        if (!IsServer) return;

        jumpsUsed.Value = 0;
        Debug.Log($"[PlayerMovementUI] {name} iniciou turno. Máx = {MaxJumps}");
        OnJumpsChanged?.Invoke();
    }

    public void EndTurn()
    {
        if (!IsServer) return;
        Debug.Log($"[PlayerMovementUI] {name} terminou turno ({jumpsUsed.Value}/{MaxJumps})");
        OnJumpsChanged?.Invoke();
    }

    public void AddExtraJumps(int amount)
    {
        if (!IsServer) return;

        extraJumps.Value += amount;
        Debug.Log($"[PlayerMovementUI] {name} ganhou {amount} pulos extras. Total: {MaxJumps}");
        OnJumpsChanged?.Invoke();
    }

    private void OnEnable()
    {
        jumpsUsed.OnValueChanged += (_, _) => OnJumpsChanged?.Invoke();
        extraJumps.OnValueChanged += (_, _) => OnJumpsChanged?.Invoke();
    }

    private void OnDisable()
    {
        jumpsUsed.OnValueChanged -= (_, _) => OnJumpsChanged?.Invoke();
        extraJumps.OnValueChanged -= (_, _) => OnJumpsChanged?.Invoke();
    }
}
