using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private int baseMaxJumps = 3; // Pulos base por turno


    private readonly List<(int extraJumps, int turnsLeft)> activeJumpPowerUps = new();

    // 🔹 Variáveis de rede
    private NetworkVariable<int> jumpsUsed = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> extraJumps = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action OnJumpsChanged;

    public int MaxJumps => baseMaxJumps + extraJumps.Value;
    public int JumpsUsed => jumpsUsed.Value;
    public int RemainingJumps => Mathf.Max(0, MaxJumps - jumpsUsed.Value);

    // ============================================================
    // 🔹 Métodos principais
    // ============================================================
    [ServerRpc(RequireOwnership = false)]
    public void ConsumeJumpServerRpc()
    {
        if (jumpsUsed.Value < MaxJumps)
        {
            jumpsUsed.Value++;
            Debug.Log($"[{OwnerClientId}] Consumiu 1 pulo. Restando {RemainingJumps}");
            NotifyClientsJumpUpdateClientRpc(jumpsUsed.Value, MaxJumps);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetJumpsServerRpc()
    {
        jumpsUsed.Value = 0;
        Debug.Log($"[{OwnerClientId}] Resetou pulos ({MaxJumps} disponíveis)");
        NotifyClientsJumpUpdateClientRpc(jumpsUsed.Value, MaxJumps);
    }

    [ClientRpc]
    private void NotifyClientsJumpUpdateClientRpc(int used, int max)
    {
        OnJumpsChanged?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddJumpPowerUpServerRpc(int extra, int duration)
    {
        if (extra <= 0) return;

        activeJumpPowerUps.Add((extra, duration));
        RecalculateExtraJumps();
        Debug.Log($"[{OwnerClientId}] Recebeu PowerUp: +{extra} pulos por {duration} turnos.");
        NotifyClientsJumpUpdateClientRpc(jumpsUsed.Value, MaxJumps);
    }

    private void RecalculateExtraJumps()
    {
        int total = 0;
        for (int i = activeJumpPowerUps.Count - 1; i >= 0; i--)
        {
            var power = activeJumpPowerUps[i];
            if (power.turnsLeft <= 0) activeJumpPowerUps.RemoveAt(i);
            else total += power.extraJumps;
        }
        extraJumps.Value = total;
    }

    public void StartTurn()
    {
        if (IsServer)
            ResetJumpsServerRpc();
    }
}
