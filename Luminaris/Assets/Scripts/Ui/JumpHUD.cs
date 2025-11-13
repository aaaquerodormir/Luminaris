using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Linq;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Configuração do Jogador")]
    [Tooltip("Índice do jogador que esta HUD representa (0 para P1, 1 para P2)")]
    [SerializeField] private int targetPlayerIndex = 0;

    [Header("Sprites")]
    [SerializeField] private Sprite[] sprites;

    private static event System.Action<ulong, int> OnJumpsCountReceived;
    private ulong targetClientId = ulong.MaxValue;
    private bool isBound = false;

    public static void NotifyJumpsChanged(ulong clientId, int remainingJumps)
    {
        OnJumpsCountReceived?.Invoke(clientId, remainingJumps);
    }

    private void OnEnable()
    {
        OnJumpsCountReceived += OnJumpsChanged;
    }

    private void OnDisable()
    {
        OnJumpsCountReceived -= OnJumpsChanged;
    }

    private void Update()
    {
        if (!isBound)
        {
            TryBindToPlayer();
        }
    }

    private void TryBindToPlayer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        var clientIds = NetworkManager.Singleton.ConnectedClientsIds.OrderBy(id => id).ToList();
        if (targetPlayerIndex < clientIds.Count)
        {
            targetClientId = clientIds[targetPlayerIndex];
            isBound = true;
        }
    }

    private void OnJumpsChanged(ulong clientId, int remainingJumps)
    {
        if (isBound && clientId == targetClientId)
        {
            UpdateDisplay(remainingJumps);
        }
    }

    private void UpdateDisplay(int jumps)
    {
        if (!jumpText || !jumpIcon) return;

        int displayJumps = Mathf.Max(0, jumps);
        jumpText.text = displayJumps.ToString();

        if (sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(displayJumps, 0, sprites.Length - 1);
            jumpIcon.sprite = sprites[index];
        }
    }
}
