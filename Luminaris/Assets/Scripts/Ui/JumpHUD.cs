using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class JumpHUD : MonoBehaviour
{
    [Header("Referências")]
    //[SerializeField] private Image player1Icon;
    [SerializeField] private PlayerMovementUI playerUI;
    [SerializeField] private Image jumpIcon;
    [SerializeField] private TextMeshProUGUI jumpText;

    [Header("Sprites por Quantidade de Pulos")]
    [SerializeField] private Sprite[] sprites;

    private bool playerFound;

    private void OnEnable()
    {
        PlayerMovementUI.OnJumpsChanged += UpdateDisplay;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        StartCoroutine(FindLocalPlayerUIRoutine());
    }

    private void OnDisable()
    {
        PlayerMovementUI.OnJumpsChanged -= UpdateDisplay;
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!playerFound)
            StartCoroutine(FindLocalPlayerUIRoutine());
    }

    private IEnumerator FindLocalPlayerUIRoutine()
    {
        yield return new WaitForSeconds(1f);

        if (playerUI == null)
        {
            var all = FindObjectsOfType<PlayerMovementUI>(true);
            foreach (var ui in all)
            {
                // 🔹 Cada HUD pega o playerUI que pertence ao dono (IsOwner = true)
                if (ui.IsOwner)
                {
                    playerUI = ui;
                    playerFound = true;
                    Debug.Log($"[JumpHUD] UI vinculada ao jogador local: {ui.name}");
                    break;
                }
            }

            if (playerUI == null)
                Debug.LogWarning("[JumpHUD] Nenhum PlayerMovementUI local encontrado — aguardando spawn...");
        }
    }

    private void UpdateDisplay(PlayerMovementUI ui, int jumps)
    {
        // 🔹 Ignora atualizações de outros jogadores
        if (playerUI == null || ui != playerUI)
            return;

        if (jumpText != null)
            jumpText.text = jumps.ToString();

        if (jumpIcon != null && sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(jumps, 0, sprites.Length - 1);
            jumpIcon.sprite = sprites[index];
        }
    }
}
