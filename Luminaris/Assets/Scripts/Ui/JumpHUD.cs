using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    //[SerializeField] private Image player1Icon;
    [SerializeField] private PlayerMovementUI linkedPlayer;  // vincule no inspector
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private Sprite[] sprites;

    private void Start()
    {
        if (linkedPlayer == null)
        {
            Debug.LogError($"[HUD:{name}] Nenhum PlayerMovementUI vinculado!");
            return;
        }

        linkedPlayer.OnJumpCountChanged += UpdateDisplay;

        // Inicializa UI com o valor atual
        UpdateDisplay(linkedPlayer.GetJumps());
    }

    private void OnDestroy()
    {
        if (linkedPlayer != null)
            linkedPlayer.OnJumpCountChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int jumps)
    {
        if (jumpText)
            jumpText.text = jumps.ToString();

        if (jumpIcon && sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(jumps, 0, sprites.Length - 1);
            jumpIcon.sprite = sprites[index];
        }

        Debug.Log($"[HUD:{name}] Atualizado → {jumps} pulos");
    }
}


