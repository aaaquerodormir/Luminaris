using UnityEngine;
using Unity.Netcode;


public class PlayerIdentifier : MonoBehaviour
{
    [Tooltip("O nome que será exibido na UI (ex: Jogador 1).")]
    public string PlayerName = "Jogador";

    [Tooltip("O sprite/imagem que representa este jogador na UI.")]
    public Sprite PlayerSprite;
}