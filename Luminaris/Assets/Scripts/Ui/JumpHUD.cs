using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class JumpHUD : NetworkBehaviour
{
    [Header("Referências")]
    //[SerializeField] private Image player1Icon;
    [SerializeField] private PlayerMovementUI player;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private Sprite[] sprites;

    private bool isLinked = false;

    private void Start()
    {
        StartCoroutine(WaitForPlayerReference());
    }

    private IEnumerator WaitForPlayerReference()
    {
        while (!isLinked)
        {
            TryLinkPlayer();
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void TryLinkPlayer()
    {
        var allPlayers = FindObjectsOfType<PlayerMovementUI>();
        foreach (var p in allPlayers)
        {
            // Usa nome para vincular automaticamente
            if (name.Contains(p.name) || (IsOwner && p.IsOwner))
            {
                player = p;
                player.OnJumpsChanged += RefreshHUD;
                RefreshHUD();
                isLinked = true;
                Debug.Log($"[JumpHUD] HUD {name} vinculado a {player.name}");
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnJumpsChanged -= RefreshHUD;
    }

    private void RefreshHUD()
    {
        if (player == null || jumpIcon == null || jumpText == null)
            return;

        int remaining = player.RemainingJumps;
        int max = player.MaxJumps;

        if (sprites != null && sprites.Length >= 3)
        {
            if (remaining <= 3)
                jumpIcon.sprite = sprites[0];
            else if (remaining == 4)
                jumpIcon.sprite = sprites[1];
            else
                jumpIcon.sprite = sprites[2];
        }

        jumpText.text = $"{remaining:00}";
    }
}
