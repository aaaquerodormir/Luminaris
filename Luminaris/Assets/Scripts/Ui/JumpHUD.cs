using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private PlayerMovementUI player;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Cor (0=até 3, 1=4, 2=5+)")]
    [SerializeField] private Sprite[] sprites;

    private void OnEnable()
    {
        // Se inscreve no novo evento OnTurnStarted usando um método adaptador.
        TurnControl.OnTurnStarted += OnTurnChanged;
        player.OnJumpsChanged += RefreshHUD;
    }

    private void OnDisable()
    {
        // Se desinscreve do evento OnTurnStarted.
        TurnControl.OnTurnStarted -= OnTurnChanged;
        player.OnJumpsChanged -= RefreshHUD;
    }

    private void Start()
    {
        RefreshHUD();
    }

    private void OnTurnChanged(PlayerMovement unusedPlayer)
    {
        RefreshHUD();
    }
    private void RefreshHUD()
    {
        if (player == null) return;

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