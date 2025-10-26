using UnityEngine;
using Unity.Netcode;
using TMPro;

[RequireComponent(typeof(NetworkObject))]
public class PowerUpColetavel : NetworkBehaviour

{
    [SerializeField] private GameObject feedBackTextualPrefab;
    [SerializeField] private PowerUpModificador powerModificador;

    [Header("Mensagem do Feedback")]
    [SerializeField] private string mensagemFeedback = "";


    // 2. 'collected' só precisa existir no servidor.
    // Evita que o servidor processe múltiplos triggers no mesmo frame.
    private bool collectedOnServer = false;

    // A lógica de 'IResettable' e 'startPos' foi removida.
    // Em Netcode, para "resetar" um item coletado, o servidor
    // deve re-spawnar o prefab do power-up, em vez de
    // reativar um objeto local.

    // 3. OnTriggerEnter2D SÓ DEVE EXECUTAR NO SERVIDOR
    private void OnTriggerEnter2D(Collider2D col)
    {
        // Apenas o servidor (Host) pode processar a coleta.
        if (!IsServer) return;

        // Se já foi coletado pelo servidor, ignora.
        if (collectedOnServer) return;

        // Verifica se é um jogador e se o modificador existe
        if (col.CompareTag("Player") && powerModificador != null)
        {
            // Tenta obter o NetworkObject do jogador para garantir
            // que é um objeto de rede válido.
            if (!col.TryGetComponent<PlayerMovement>(out var player))
            {
                Debug.LogWarning($"[PowerUpColetavel] Objeto {col.name} tem tag 'Player' mas não tem script 'PlayerMovement'.");
                return;
            }

            // 4. LÓGICA AUTORITÁRIA (SERVER)

            // Marca como coletado (no servidor)
            collectedOnServer = true;
            Debug.Log($"[PowerUp-SERVER] {col.name} (Client: {player.OwnerClientId}) coletou {gameObject.name}");

            // Aplica o efeito (no servidor).
            // O ScriptableObject (JumpPowerUp/LavaSpeedPowerUp)
            // executará no servidor, modificando o estado de rede
            // do PlayerMovement ou do LavaRise.
            powerModificador.Activate(col.gameObject);

            // 5. DISPARA FEEDBACK EM TODOS OS CLIENTES
            Vector3 feedbackPos = transform.position + Vector3.up * 1f;

            // Passa o ID do cliente que pegou, caso queira customizar a msg
            // (Ex: "Você pegou!" vs "Player 2 pegou!")
            NotifyClientsOfPickupClientRpc(player.OwnerClientId, mensagemFeedback, feedbackPos);

            // 6. DESPAWNA O OBJETO DA REDE
            // Isso remove o power-up para TODOS os jogadores.
            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn();
            }
            else
            {
                // Fallback para testes (se não foi spawnado pela rede)
                gameObject.SetActive(false);
            }
        }
    }

    // 7. CLIENT RPC PARA FEEDBACK
    [ClientRpc]
    private void NotifyClientsOfPickupClientRpc(ulong collectingClientId, string mensagem, Vector3 posicao)
    {
        // Este código executa em TODOS os clientes.

        // Toca o som localmente
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("PowerUp");
        }

        // Mostra o texto de feedback visual localmente
        // Você pode usar o 'collectingClientId' para customizar a mensagem
        // ex: if (collectingClientId == NetworkManager.Singleton.LocalClientId) { ... }
        ShowFeedback(mensagem, posicao);
    }

    // Esta função é local e chamada pelo ClientRpc
    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        // Instancia o feedback localmente.
        // Isso NÃO precisa ser um NetworkObject.
        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);
        var textComp = temp.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }
}
