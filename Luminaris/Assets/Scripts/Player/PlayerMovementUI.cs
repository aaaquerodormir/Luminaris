using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    [Header("Configuração")]
    [SerializeField] private int maxJumps = 3;

    // 🔹 Variável de rede simples e confiável
    private NetworkVariable<int> remainingJumps = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 🔹 Callback local (para a HUD)
    public event Action<int> OnJumpCountChanged;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            remainingJumps.Value = maxJumps;

        remainingJumps.OnValueChanged += OnJumpsChanged;

        Debug.Log($"[PlayerUI:{name}] Spawned | Owner={OwnerClientId} | Server={IsServer}");
    }

    private void OnDestroy()
    {
        remainingJumps.OnValueChanged -= OnJumpsChanged;
    }

    private void OnJumpsChanged(int oldValue, int newValue)
    {
        Debug.Log($"[SYNC:{name}] Jumps mudou {oldValue} → {newValue}");
        OnJumpCountChanged?.Invoke(newValue);
    }

    // 🔹 Apenas o Host altera o valor — clients recebem automaticamente
    [ServerRpc(RequireOwnership = false)]
    public void UpdateJumpCountServerRpc(int newCount)
    {
        remainingJumps.Value = newCount;
    }

    // 🔹 Acesso público simples para PlayerMovement
    public int GetJumps() => remainingJumps.Value;
    public void SetJumps(int newCount)
    {
        if (IsServer)
            remainingJumps.Value = newCount;
        else
            UpdateJumpCountServerRpc(newCount);
    }
}
