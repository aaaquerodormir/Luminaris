using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider2D))] // Garante que o jogador tenha um collider
public class PlayerState : NetworkBehaviour
{
    [Header("Configuração da Plataforma Segura")]
    [Tooltip("A Tag usada nas plataformas seguras (Plataforma Y)")]
    [SerializeField]
    private string safePlatformTag = "SafePlatform";
    public readonly NetworkVariable<bool> IsOnSafePlatform = new(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    // ==============================================================
    // ==== LÓGICA DA PLATAFORMA SEGURA (PLATAFORMA Y) ====
    // ==============================================================

    // NOTA: Configure sua "Plataforma Y" (segura) com:
    // 1. Um Collider2D
    // 2. Marque a caixa "Is Trigger"
    // 3. Atribua a Tag "SafePlatform" (ou a tag que você definiu acima)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Apenas o Owner (dono) do jogador detecta e reporta ao servidor
        if (!IsOwner) return;

        if (collision.CompareTag(safePlatformTag))
        {
            // Informa ao servidor que está na plataforma segura
            UpdateSafePlatformStatusServerRpc(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Apenas o Owner detecta e reporta ao servidor
        if (!IsOwner) return;

        if (collision.CompareTag(safePlatformTag))
        {
            // Informa ao servidor que saiu da plataforma segura
            UpdateSafePlatformStatusServerRpc(false);
        }
    }
    [ServerRpc]
    private void UpdateSafePlatformStatusServerRpc(bool status)
    {
        IsOnSafePlatform.Value = status;
        Debug.Log($"[PlayerState-SERVER] Jogador {OwnerClientId} está em plataforma segura: {status}");
    }
}