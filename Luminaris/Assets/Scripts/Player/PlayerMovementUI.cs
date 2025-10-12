using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private int baseMaxJumps = 3; // Pulos base por turno

    private readonly List<(int extraJumps, int turnsLeft)> activeJumpPowerUps = new();

    // NetworkVariable para sincronizar o número de pulos usados
    private NetworkVariable<int> jumpsUsed = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> currentMaxJumps = new(3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action OnJumpsChanged;

    public int MaxJumps => currentMaxJumps.Value;
    public int JumpsUsed => jumpsUsed.Value;
    public int RemainingJumps => Mathf.Max(0, currentMaxJumps.Value - jumpsUsed.Value);

    public override void OnNetworkSpawn()
    {
        jumpsUsed.OnValueChanged += (_, _) => OnJumpsChanged?.Invoke();
        currentMaxJumps.OnValueChanged += (_, _) => OnJumpsChanged?.Invoke();
    }

    // ======================================================
    // ====== LÓGICA DE PULOS ================================
    // ======================================================
    [ServerRpc(RequireOwnership = false)]
    public void ConsumeJumpServerRpc()
    {
        if (jumpsUsed.Value < currentMaxJumps.Value)
        {
            jumpsUsed.Value++;
            OnJumpsChanged?.Invoke();
            Debug.Log($"[PlayerUI] {OwnerClientId} consumiu 1 pulo ({RemainingJumps} restantes).");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetJumpsServerRpc()
    {
        jumpsUsed.Value = 0;
        currentMaxJumps.Value = baseMaxJumps + GetTotalExtraJumps();
        OnJumpsChanged?.Invoke();
        Debug.Log($"[PlayerUI] {OwnerClientId} resetou pulos. Máximo: {currentMaxJumps.Value}");
    }

    // ======================================================
    // ====== POWER UPS =====================================
    // ======================================================
    [ServerRpc(RequireOwnership = false)]
    public void AddJumpPowerUpServerRpc(int extraJumps, int duration)
    {
        if (extraJumps <= 0) return;

        activeJumpPowerUps.Add((extraJumps, duration));
        currentMaxJumps.Value = baseMaxJumps + GetTotalExtraJumps();

        OnJumpsChanged?.Invoke();
        Debug.Log($"[PlayerUI] PowerUp: +{extraJumps} pulos ({duration} turnos). Max agora = {currentMaxJumps.Value}");
    }

    public void StartTurn()
    {
        if (!IsServer) return;

        // Remove power-ups expirados
        for (int i = activeJumpPowerUps.Count - 1; i >= 0; i--)
        {
            var p = activeJumpPowerUps[i];
            p.turnsLeft--;
            if (p.turnsLeft <= 0)
                activeJumpPowerUps.RemoveAt(i);
            else
                activeJumpPowerUps[i] = p;
        }

        ResetJumpsServerRpc();
    }

    public void EndTurn()
    {
        if (IsServer)
            Debug.Log($"[PlayerUI] Player {OwnerClientId} terminou o turno com {RemainingJumps} pulos restantes.");
    }

    private int GetTotalExtraJumps()
    {
        int total = 0;
        foreach (var p in activeJumpPowerUps)
            total += p.extraJumps;
        return total;
    }
}
