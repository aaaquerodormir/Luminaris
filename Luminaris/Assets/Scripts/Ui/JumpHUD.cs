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

    [Header("HUD Configuração")]
    [Tooltip("Marque se esta HUD representa o jogador host (Player1).")]
    [SerializeField] private bool isHostHUD;


    private bool playerFound;

    private void OnEnable()
    {
        PlayerMovementUI.OnJumpsChanged += UpdateDisplay;

        // 🔹 Escuta o spawn real vindo do servidor
        CustomPlayerSpawner.OnPlayerSpawned += HandlePlayerSpawned;

        // 🔹 Escuta conexões (útil no client)
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        PlayerMovementUI.OnJumpsChanged -= UpdateDisplay;
        CustomPlayerSpawner.OnPlayerSpawned -= HandlePlayerSpawned;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        // Reforço: tenta encontrar o jogador novamente após nova conexão
        if (!playerFound)
            StartCoroutine(TryFindPlayerDelayed());
    }

    private void HandlePlayerSpawned(ulong clientId, PlayerMovementUI ui)
    {
        // Vincula quando o player é realmente spawnado
        var id = ui.GetComponent<PlayerIdentifier>();
        if (id == null) return;

        if (id.IsHostPlayer == isHostHUD)
        {
            playerUI = ui;
            playerFound = true;
            Debug.Log($"[JumpHUD:{name}] Vinculado automaticamente ao jogador {(isHostHUD ? "HOST" : "CLIENT")} ({ui.name})");
        }
    }

    private IEnumerator TryFindPlayerDelayed()
    {
        yield return new WaitForSeconds(1f);

        if (playerFound) yield break;

        var all = FindObjectsOfType<PlayerMovementUI>(true);
        foreach (var ui in all)
        {
            var id = ui.GetComponent<PlayerIdentifier>();
            if (id == null) continue;

            if (id.IsHostPlayer == isHostHUD)
            {
                playerUI = ui;
                playerFound = true;
                Debug.Log($"[JumpHUD:{name}] Encontrado manualmente o jogador {(isHostHUD ? "HOST" : "CLIENT")} ({ui.name})");
                break;
            }
        }

        if (!playerFound)
            Debug.LogWarning($"[JumpHUD:{name}] Nenhum jogador correspondente encontrado mesmo após delay.");
    }

    private void UpdateDisplay(PlayerMovementUI ui, int jumps)
    {
        if (playerUI == null || ui != playerUI) return;

        if (jumpText != null)
            jumpText.text = jumps.ToString();

        if (jumpIcon != null && sprites != null && sprites.Length > 0)
        {
            int index = Mathf.Clamp(jumps, 0, sprites.Length - 1);
            jumpIcon.sprite = sprites[index];
        }
    }
}
