using UnityEngine;
using Unity.Netcode;


public class PlayerIdentifier : NetworkBehaviour
{
    [Tooltip("Nome que será exibido na UI (ex: Jogador 1).")]
    public string PlayerName = "Jogador";

    [Tooltip("Sprite/imagem que representa este jogador na UI.")]
    public Sprite PlayerSprite;

    [Header("Identificação de Rede")]
    [SerializeField] private bool isHostPlayer;
    public bool IsHostPlayer => isHostPlayer;

    public override void OnNetworkSpawn()
    {
        isHostPlayer = IsServer;
        Debug.Log($"[PlayerIdentifier:{name}] Registrado como {(isHostPlayer ? "Host" : "Client")}");
    }
}