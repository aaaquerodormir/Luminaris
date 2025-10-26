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

    // 🔑 O evento DEVE ser privado, apenas o MÉTODO ESTATICO o dispara.
    private static event System.Action<ulong, int> OnJumpsCountReceived;

    // Ponto de entrada público para disparar o evento (chamado pelo PlayerMovement)
    public static void NotifyJumpsChanged(ulong clientId, int remainingJumps)
    {
        OnJumpsCountReceived?.Invoke(clientId, remainingJumps);
    }

    private ulong targetClientId = ulong.MaxValue;
    private bool isBound = false;

    private void OnEnable()
    {
        // ✅ CORRIGIDO: Removemos a linha "CustomPlayerSpawner.OnPlayerSpawned += TryBindOnSpawn;"
        OnJumpsCountReceived += OnJumpsChanged;

        // 🔑 NOVO: Agora, tentamos vincular APENAS quando o componente é ativado ou no Start
        // A lógica de TryBindToPlayer tentará encontrar o NetworkManager e os jogadores.
    }

    private void OnDisable()
    {
        // ✅ CORRIGIDO: Removemos a linha "CustomPlayerSpawner.OnPlayerSpawned -= TryBindOnSpawn;"
        OnJumpsCountReceived -= OnJumpsChanged;
    }

    private void Start()
    {
        // Tentativa inicial de vinculação (para o caso de jogadores já terem spawnado)
        TryBindToPlayer();
    }

    // A função TryBindOnSpawn() foi REMOVIDA, pois não é mais necessária!

    // Tenta vincular com base no índice e ID
    private void TryBindToPlayer()
    {
        if (isBound || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) return;

        // 1. Determina o ClientId que esta HUD DEVE rastrear.
        var clients = NetworkManager.Singleton.ConnectedClientsList;
        var clientIds = clients.Select(c => c.ClientId).OrderBy(id => id).ToArray();

        if (targetPlayerIndex >= clientIds.Length)
        {
            // Ainda não há jogadores suficientes para este índice. Tente novamente mais tarde.
            Debug.LogWarning($"[JumpHUD:{name}] Nenhum client com índice {targetPlayerIndex} encontrado ainda.");
            return;
        }

        // O ID do cliente que esta HUD deve rastrear.
        targetClientId = clientIds[targetPlayerIndex];

        // 2. Itera sobre todos os NetworkObjects para encontrar o jogador.
        // Isso é seguro para Clientes e Servidores, pois NetworkObject.IsSpawned é global.

        // 💡 IMPORTANTE: Estamos procurando o objeto PlayerMovement.
        var allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (var playerMovement in allPlayers)
        {
            // 3. Verifica se este objeto PlayerMovement pertence ao ClientId que estamos rastreando.
            if (playerMovement.NetworkObject.OwnerClientId == targetClientId)
            {
                // Vinculação bem-sucedida!
                isBound = true;
                Debug.Log($"[JumpHUD:{name}] ✅ Vinculado ao Player {targetPlayerIndex} (ID: {targetClientId}, Objeto: {playerMovement.name}).");

                // Garante que o display inicial seja 0 ou o valor sincronizado atual
                UpdateDisplay(targetClientId, playerMovement.CompletedJumpsNet.Value);
                return;
            }
        }
    }

    // O Callback que recebe a informação de Pulos
    private void OnJumpsChanged(ulong clientId, int remainingJumps)
    {
        // Apenas atualiza se o ID do cliente corresponder ao ID que esta HUD está seguindo
        if (clientId == targetClientId)
        {
            UpdateDisplay(clientId, remainingJumps);
        }
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



