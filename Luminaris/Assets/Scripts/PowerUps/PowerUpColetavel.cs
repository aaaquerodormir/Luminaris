using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PowerUpColetavel : NetworkBehaviour
{
    [SerializeField] private GameObject feedBackTextualPrefab;
    [SerializeField] private PowerUpModificador powerModificador;

    [Header("Mensagem do Feedback")]
    [SerializeField] private string mensagemFeedback = "";

    [SerializeField] private float pickupValidateRadius = 2f; // não crítico, só pra evitar pegar do outro lado do mapa

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        if (powerModificador == null) return;

        var player = col.GetComponentInParent<PlayerMovement>();
        if (player == null) return;

        // Se estamos no servidor aplicamos direto
        if (IsServer)
        {
            ApplyOnServer(player);
            return;
        }

        // Cliente: solicita ao servidor a coleta
        RequestCollectServerRpc(player.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCollectServerRpc(ulong playerOwnerClientId, ServerRpcParams rpcParams = default)
    {
        // Simples: encontramos o player no servidor e aplicamos.
        var all = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        PlayerMovement target = null;
        foreach (var p in all)
            if (p.NetworkObject.OwnerClientId == playerOwnerClientId) { target = p; break; }

        if (target == null) return;

        // Opcional: valida distância pra reduzir flagrante de cheat (não obrigatório)
        float d = Vector3.Distance(transform.position, target.transform.position);
        if (d > pickupValidateRadius) return;

        ApplyOnServer(target);
    }

    private void ApplyOnServer(PlayerMovement target)
    {
        // 1) Aplica efeito (no servidor) — powerModificador cuida de chamar os métodos do Player
        powerModificador.Activate(target.gameObject);

        // 2) Notifica clients para mostrar feedback
        ShowFeedbackClientRpc(mensagemFeedback, transform.position + Vector3.up * 1f);
        PlaySoundClientRpc("PowerUp");

        // 3) Despawn do objeto (servidor)
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn(true);
        else
            gameObject.SetActive(false);
    }

    [ClientRpc]
    private void ShowFeedbackClientRpc(string mensagem, Vector3 pos)
    {
        if (feedBackTextualPrefab == null) return;
        GameObject t = Instantiate(feedBackTextualPrefab, pos, Quaternion.identity);
        var txt = t.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (txt != null) txt.text = mensagem;
        t.transform.SetParent(null);
        Destroy(t, 1.5f);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(string sound)
    {
        AudioManager.Instance?.PlaySound(sound);
    }
}