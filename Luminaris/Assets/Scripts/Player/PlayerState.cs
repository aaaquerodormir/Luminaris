using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))] 
public class PlayerState : NetworkBehaviour
{
    [Header("Configuração da Plataforma Segura")]
    [Tooltip("A Tag usada nas plataformas seguras (Plataforma Y)")]
    [SerializeField]
    private string safePlatformTag = "SafePlatform";
    public readonly NetworkVariable<bool> IsOnSafePlatform = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag(safePlatformTag))
        {
            UpdateSafePlatformStatusServerRpc(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.CompareTag(safePlatformTag))
        {
            UpdateSafePlatformStatusServerRpc(false);
        }
    }
    [ServerRpc]
    private void UpdateSafePlatformStatusServerRpc(bool status)
    {
        IsOnSafePlatform.Value = status;
    }
}