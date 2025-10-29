using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
public class VisionFollower : NetworkBehaviour
{
    private RectTransform visionMaskRect;
    private Canvas canvas;

    private Camera mainCamera;
    private DebuffVisionControl debuffControl;

    private void Start()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }
        mainCamera = Camera.main;
        debuffControl = GetComponent<DebuffVisionControl>();

        if (UIManager.Instance != null)
        {
            visionMaskRect = UIManager.Instance.VisionMaskRect;
            canvas = UIManager.Instance.MainCanvas;
        }
        else
        {
            Debug.LogError("[VisionFollower] UIManager Singleton não encontrado na cena!");
            enabled = false; // Desativa se a UI não puder ser referenciada
            return;
        }


        if (mainCamera == null)
        {
            Debug.LogError("[VisionFollower] Camera.main não encontrada. Certifique-se de que a câmera principal está taggeada como 'MainCamera'.");
        }

        if (debuffControl == null)
        {
            Debug.LogError("[VisionFollower] DebuffVisionControl não encontrado no Prefab do Jogador.");
        }
    }

    private void Update()
    {
        // Só executa se o debuff estiver ativo E as referências tiverem sido encontradas
        if (debuffControl == null || !debuffControl.IsDebuffed() || visionMaskRect == null || canvas == null)
        {
            return;
        }

        Vector3 worldPosition = transform.position;
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);

        // Determina qual câmera usar na conversão (baseado no Render Mode do Canvas)
        Camera usedCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? mainCamera : null;

        Vector2 localPoint;

        // Converte a posição da tela para a posição local do RectTransform do Canvas
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            visionMaskRect.parent as RectTransform,
            screenPoint,
            usedCamera,
            out localPoint))
        {
            // Move o RectTransform do Vision Mask (o buraco) para a posição do jogador
            visionMaskRect.localPosition = localPoint;
        }
    }
}