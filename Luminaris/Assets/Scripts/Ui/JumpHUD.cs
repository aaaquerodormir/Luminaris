using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;
using System.Linq;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private Sprite[] sprites;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private int targetPlayerIndex = 0; // 0 = P1, 1 = P2, etc.

    private static event System.Action<ulong, int> OnJumpsCountReceived;

    public static void NotifyJumpsChanged(ulong clientId, int remainingJumps)
    {
        OnJumpsCountReceived?.Invoke(clientId, remainingJumps);
    }

    private ulong targetClientId = ulong.MaxValue;
    private bool isBound = false;

    private void OnEnable()
    {
        OnJumpsCountReceived += OnJumpsChanged;
    }

    private void OnDisable()
    {
        OnJumpsCountReceived -= OnJumpsChanged;
    }

    private void Start()
    {
        TryBindToPlayer();
    }

    private void TryBindToPlayer()
    {
        if (isBound || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        var clients = NetworkManager.Singleton.ConnectedClientsList;
        var clientIds = clients.Select(c => c.ClientId).OrderBy(id => id).ToArray();

        if (targetPlayerIndex >= clientIds.Length)
        {
            Debug.LogWarning($"[JumpHUD:{name}] Nenhum client com índice {targetPlayerIndex} encontrado ainda.");
            return;
        }

        targetClientId = clientIds[targetPlayerIndex];

        var allPlayers = FindObjectsOfType<PlayerMovement>();
        foreach (var playerMovement in allPlayers)
        {
            if (playerMovement.NetworkObject.OwnerClientId == targetClientId)
            {
                isBound = true;
                Debug.Log($"[JumpHUD:{name}] ✅ Vinculado ao Player {targetPlayerIndex} (ID: {targetClientId}, Objeto: {playerMovement.name}).");
                UpdateDisplay(targetClientId, playerMovement.GetComponent<PlayerMovement>()?.GetMaxJumps() ?? 0);
                return;
            }
        }
    }

    private void OnJumpsChanged(ulong clientId, int remainingJumps)
    {
        if (clientId == targetClientId)
            UpdateDisplay(clientId, remainingJumps);
    }

    private void UpdateDisplay(ulong clientId, int jumps)
    {
        if (!jumpText || !jumpIcon) return;

        jumpText.text = jumps.ToString();

        if (sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(jumps, 0, sprites.Length - 1);
            jumpIcon.sprite = sprites[index];
        }

        Debug.Log($"[HUD:{name}] Atualizado → Client ID {clientId} ({jumps} pulos restantes)");
    }
}


