using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Cor (0=até 3, 1=4, 2=5+)")]
    [SerializeField] private Sprite[] sprites;

    private void OnEnable()
    {
        if (playerUI != null)
            playerUI.OnJumpsChanged += RefreshHUD;

        TurnControl.OnTurnStarted += OnTurnChanged;
    }

    private void OnDisable()
    {
        if (playerUI != null)
            playerUI.OnJumpsChanged -= RefreshHUD;

        TurnControl.OnTurnStarted -= OnTurnChanged;
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void OnTurnChanged(PlayerMovement player)
    {
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (playerUI == null || jumpIcon == null || jumpText == null)
            return;

        int remaining = playerUI.RemainingJumps;
        int max = playerUI.MaxJumps;

        // Atualiza sprite conforme a quantidade de pulos
        if (sprites != null && sprites.Length >= 3)
        {
            if (max <= 3)
                jumpIcon.sprite = sprites[0];
            else if (max == 4)
                jumpIcon.sprite = sprites[1];
            else
                jumpIcon.sprite = sprites[2];
        }

        // Atualiza texto
        jumpText.text = $"{remaining:00}";
    }
}