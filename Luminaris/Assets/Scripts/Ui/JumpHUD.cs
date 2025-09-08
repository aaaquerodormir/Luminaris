using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JumpHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovementUI player;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por cor (0=até 3, 1=4, 2=5+)")]
    [SerializeField] private Sprite[] sprites;

    private void OnEnable()
    {
        TurnControl.OnTurnEnded += RefreshHUD;
        player.OnJumpsChanged += RefreshHUD;
    }

    private void OnDisable()
    {
        TurnControl.OnTurnEnded -= RefreshHUD;
        player.OnJumpsChanged -= RefreshHUD;
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        int remainingJumps = player.MaxJumps - player.JumpsUsed;

        if (sprites != null && sprites.Length >= 3)
        {
            if (remainingJumps <= 3)
                jumpIcon.sprite = sprites[0];
            else if (remainingJumps == 4)
                jumpIcon.sprite = sprites[1];
            else
                jumpIcon.sprite = sprites[2];
        }

        if (jumpText != null)
            jumpText.text = $"{remainingJumps:00}";
    }
}
