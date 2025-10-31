using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
// Se precisar de funcionalidades espec�ficas do Cinemachine, adicione a linha abaixo:
//using Cinemachine; 

public class VisionFollower : NetworkBehaviour
{
    private RectTransform visionMaskRect;
    private Canvas canvas;

    private Camera localPlayerCamera;
    private DebuffVisionControl debuffControl;

    [Header("Limita��o da Borda")]
    [SerializeField]
    private float screenEdgeBuffer = 10f; // Margem de seguran�a (em pixels) para a borda da tela.

    public override void OnNetworkSpawn()
    {
        // Garante que o script s� execute para o cliente que � o Dono (Dono do Prefab)
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        debuffControl = GetComponent<DebuffVisionControl>();
        localPlayerCamera = GetComponentInChildren<Camera>(true);

        if (UIManager.Instance != null)
        {
            visionMaskRect = UIManager.Instance.VisionMaskRect;
            canvas = UIManager.Instance.MainCanvas;
        }
        else
        {
            Debug.LogError("[VisionFollower] UIManager Singleton n�o encontrado na cena! A UI deve estar na cena.");
            enabled = false;
            return;
        }

        if (localPlayerCamera == null)
        {
            Debug.LogError("[VisionFollower] C�mera Local (Filha) n�o encontrada. Certifique-se de que a c�mera est� dentro do Prefab do Jogador.");
            enabled = false;
        }
    }

    private void Update()
    {
        // Usa localPlayerCamera em vez de mainCamera
        if (debuffControl == null || !debuffControl.IsDebuffed() || visionMaskRect == null || canvas == null || localPlayerCamera == null)
        {
            return;
        }

        // 1. Converte a posi��o 3D do jogador para a posi��o 2D da tela.
        Vector3 worldPosition = transform.position;
        // Usa a c�mera local
        Vector3 screenPoint = localPlayerCamera.WorldToScreenPoint(worldPosition);
        // Captura as dimens�es atuais da viewport da C�mera LOCAL (seja tela cheia ou tela dividida)
        float viewportWidth = localPlayerCamera.pixelWidth;
        float viewportHeight = localPlayerCamera.pixelHeight;

        // Calcula o raio da m�scara, que serve como limite m�nimo para o Clamp.
        float maskHalfSize = Mathf.Min(visionMaskRect.rect.width, visionMaskRect.rect.height) / 2f;

        // O buffer total � o raio da m�scara + a margem de seguran�a ajust�vel.
        float buffer = maskHalfSize + screenEdgeBuffer;

        // Limita o screenPoint.x: O centro deve parar antes da borda lateral.
        screenPoint.x = Mathf.Clamp(
            screenPoint.x,
            buffer,
            viewportWidth - buffer
        );

        // Limita o screenPoint.y: O centro deve parar antes da borda superior/inferior.
        screenPoint.y = Mathf.Clamp(
            screenPoint.y,
            buffer,
            viewportHeight - buffer
        );

        // ---------------------------------------------

        // 2. Converte a posi��o da tela (AGORA LIMITADA) para a posi��o local do Canvas
        // Usa a c�mera local
        Camera usedCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? localPlayerCamera : null;

        Vector2 localPoint;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            visionMaskRect.parent as RectTransform,
            screenPoint,
            usedCamera,
            out localPoint))
        {
            visionMaskRect.localPosition = localPoint;
        }
    }

}