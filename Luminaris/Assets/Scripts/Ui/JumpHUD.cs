using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class JumpHUD : NetworkBehaviour
{
    [Header("Referências")]
    [SerializeField] private Image player1Icon;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private Sprite[] sprites;

    [Header("Sincronização")]
    [SerializeField] private bool showRemotePlayers = true;
    // se ativado, exibe também o estado de players que não são o dono local

    private PlayerMovementUI[] playerUIs;

    private void Start()
    {
        InvokeRepeating(nameof(UpdateHUD), 1f, 0.3f); // atualiza a cada 0.3s
    }

    private void UpdateHUD()
    {
        if (playerUIs == null || playerUIs.Length == 0)
            playerUIs = FindObjectsOfType<PlayerMovementUI>().OrderBy(p => p.OwnerClientId).ToArray();

        if (playerUIs.Length < 2) return;

        UpdateForPlayer(playerUIs[0], player1Icon, player1Text);
        UpdateForPlayer(playerUIs[1], player2Icon, player2Text);
    }

    private void UpdateForPlayer(PlayerMovementUI ui, Image icon, TextMeshProUGUI text)
    {
        if (ui == null || icon == null || text == null) return;

        int remaining = ui.RemainingJumps;
        int max = ui.MaxJumps;

        if (sprites != null && sprites.Length >= 3)
        {
            if (remaining <= 3) icon.sprite = sprites[0];
            else if (remaining == 4) icon.sprite = sprites[1];
            else icon.sprite = sprites[2];
        }

        text.text = $"{remaining:00}";
    }
}