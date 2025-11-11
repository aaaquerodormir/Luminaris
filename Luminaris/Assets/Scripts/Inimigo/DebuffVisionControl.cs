using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DebuffVisionControl : NetworkBehaviour
{
    // A máscara de UI local (atribuída pelo VisionFollower)
    private Image visionMaskImage;

    [Header("Debuff Settings")]
    [Tooltip("Duração do Fade In/Out em segundos.")]
    [SerializeField] private float fadeDuration = 0.5f;

    private readonly NetworkVariable<int> turnsRemaining = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void SetLocalMask(Image mask)
    {
        visionMaskImage = mask;
        if (visionMaskImage != null)
        {

            float targetAlpha = (turnsRemaining.Value > 0) ? 0.98f : 0f;
            visionMaskImage.color = new Color(1f, 1f, 1f, targetAlpha);
        }
    }

    public override void OnNetworkSpawn()
    {
        turnsRemaining.OnValueChanged += OnDebuffStateChanged;

        // Garante o estado visual correto se a máscara já foi atribuída
        if (visionMaskImage != null)
        {
            float targetAlpha = (turnsRemaining.Value > 0) ? 0.98f : 0f;
            visionMaskImage.color = new Color(1f, 1f, 1f, targetAlpha);
        }

        // Lógica do servidor continua igual
        if (IsServer)
        {
            TurnControl.OnTurnStarted += OnTurnStartedHandler;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Limpa a inscrição
        turnsRemaining.OnValueChanged -= OnDebuffStateChanged;

        if (IsServer)
        {
            TurnControl.OnTurnStarted -= OnTurnStartedHandler;
        }
    }

    private void OnDebuffStateChanged(int previousValue, int newValue)
    {
        if (visionMaskImage == null)
        {
            return;
        }

        bool isDebuffed = newValue > 0;
        bool wasDebuffed = previousValue > 0;

        // Iniciar o fade IN (só se não estava com debuff antes)
        if (isDebuffed && !wasDebuffed)
        {
            StartCoroutine(FadeCoroutine(true));
        }
        // Iniciar o fade OUT (só se estava com debuff e agora não está)
        else if (!isDebuffed && wasDebuffed)
        {
            StartCoroutine(FadeCoroutine(false));
        }
    }

    public void StartDebuffServer(int durationTurns, ulong targetClientId)
    {
        if (!IsServer || OwnerClientId != targetClientId) return;

        // Apenas atualiza a NetworkVariable. Todos os clientes
        // reagirão a esta mudança através do 'OnValueChanged'.
        turnsRemaining.Value = durationTurns;
    }

    private void OnTurnStartedHandler(PlayerMovement newActivePlayer)
    {
        if (!IsServer) return;

        // "O ID do novo jogador é o mesmo ID do dono deste script?"
        // Se sim, o turno é do jogador que tem este debuff.
        if (newActivePlayer.OwnerClientId == OwnerClientId)
        {
            if (turnsRemaining.Value > 0)
            {
                turnsRemaining.Value--; // Diminui o contador
            }
        }
    }

    public bool IsDebuffed()
    {
        return turnsRemaining.Value > 0;
    }

    private IEnumerator FadeCoroutine(bool fadeIn)
    {
        float targetAlpha = fadeIn ? 0.98f : 0f;

        // Evita erro se a máscara ainda não foi definida
        if (visionMaskImage == null) yield break;

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