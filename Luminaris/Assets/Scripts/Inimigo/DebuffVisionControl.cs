using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class DebuffVisionControl : NetworkBehaviour
{

    private Image visionMaskImage;

    [Header("Debuff Settings")]
    [Tooltip("Duração do Fade In/Out em segundos.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly NetworkVariable<int> turnsRemaining = new(
        0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);
    private void Start()
    {
        if (UIManager.Instance != null && visionMaskImage == null)
        {
            visionMaskImage = UIManager.Instance.VisionMaskImage;
        }
        else if (UIManager.Instance == null)
        {
            Debug.LogError("[DebuffVisionControl] UIManager Singleton não encontrado na cena!");
        }
    }


    public override void OnNetworkSpawn()
    {
        if (visionMaskImage == null)
        {
            Start();
        }

        if (visionMaskImage != null)
        {
            visionMaskImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (IsServer)
        {
            TurnControl.OnTurnStarted += OnTurnStartedHandler;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TurnControl.OnTurnStarted -= OnTurnStartedHandler;
        }
    }

    // Chamado pelo EnemyController.cs
    public void StartDebuffServer(int durationTurns, ulong targetClientId)
    {
        // Se não for o servidor OU se o objeto não for o do cliente alvo, ignora.
        if (!IsServer || OwnerClientId != targetClientId) return;

        turnsRemaining.Value = durationTurns;

        // Envia RPC de Fade IN apenas para o cliente alvo (Sintaxe Corrigida)
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { targetClientId }
            }
        };

        ApplyFadeClientRpc(true, rpcParams);
    }

    // Assumindo que este método é chamado pelo evento TurnControl.OnTurnStarted
    private void OnTurnStartedHandler(PlayerMovement newActivePlayer)
    {
        if (!IsServer) return;

        // Verifica se o jogador que está começando o turno é o DONO deste script
        if (newActivePlayer.OwnerClientId == OwnerClientId)
        {
            if (turnsRemaining.Value > 0)
            {
                turnsRemaining.Value--;

                if (turnsRemaining.Value == 0)
                {
                    // Debuff terminou. Envia RPC de Fade OUT
                    var rpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                    ApplyFadeClientRpc(false, rpcParams);
                }
            }
        }
    }

    // Método que o VisionFollower usa para saber se deve mover a máscara
    public bool IsDebuffed()
    {
        // Só faz sentido verificar se somos o jogador local
        return IsOwner && turnsRemaining.Value > 0;
    }
    [ClientRpc]
    private void ApplyFadeClientRpc(bool fadeIn, ClientRpcParams rpcParams = default)
    {
        // Executado APENAS no PC do jogador alvo
        if (visionMaskImage == null) return;

        StartCoroutine(FadeCoroutine(fadeIn));
    }

    private IEnumerator FadeCoroutine(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        float startAlpha = visionMaskImage.color.a;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            visionMaskImage.color = new Color(1f, 1f, 1f, newAlpha);
            yield return null;
        }

        visionMaskImage.color = new Color(1f, 1f, 1f, targetAlpha);
    }
}