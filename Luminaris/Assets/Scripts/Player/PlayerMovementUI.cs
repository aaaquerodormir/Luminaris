using UnityEngine;
using Unity.Netcode;

public class PlayerMovementUI : NetworkBehaviour
{
    private readonly NetworkVariable<int> remainingJumpsNet = new(
        3, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        remainingJumpsNet.OnValueChanged += OnRemainingJumpsChanged;

        if (IsServer)
        {
            // Sincroniza o valor inicial caso o jogador entre depois
            var playerMovement = GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                UpdateJumps(playerMovement.MaxJumpsNet.Value, playerMovement.CompletedJumpsNet.Value);
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        remainingJumpsNet.OnValueChanged -= OnRemainingJumpsChanged;
    }

    private void OnRemainingJumpsChanged(int previousValue, int newValue)
    {
        JumpHUD.NotifyJumpsChanged(OwnerClientId, newValue);
    }

    public void UpdateJumps(int maxJumps, int completedJumps)
    {
        if (!IsServer) return;
        remainingJumpsNet.Value = maxJumps - completedJumps;
    }
}
