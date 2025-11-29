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

    private bool collectedOnServer = false;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) return;

        if (collectedOnServer) return;

        if (col.CompareTag("Player") && powerModificador != null)
        {
            if (!col.TryGetComponent<PlayerMovement>(out var player))
            {
                return;
            }

            collectedOnServer = true;

            powerModificador.Activate(col.gameObject);

            Vector3 feedbackPos = transform.position + Vector3.up * 1f;
            NotifyClientsOfPickupClientRpc(player.OwnerClientId, mensagemFeedback, feedbackPos);

            if (NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
    [ClientRpc]
    private void NotifyClientsOfPickupClientRpc(ulong collectingClientId, string mensagem, Vector3 posicao)
    {

        if (TryGetComponent<Collider2D>(out var col))
            col.enabled = false;
        if (TryGetComponent<SpriteRenderer>(out var rend))
            rend.enabled = false;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySound("PowerUp");
        }

        ShowFeedback(mensagem, posicao);
    }
    private void ShowFeedback(string mensagem, Vector3 posicao)
    {
        if (feedBackTextualPrefab == null) return;

        GameObject temp = Instantiate(feedBackTextualPrefab, posicao, Quaternion.identity);
        var textComp = temp.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
            textComp.text = mensagem;

        temp.transform.SetParent(null);
        Destroy(temp, 1.5f);
    }
}
