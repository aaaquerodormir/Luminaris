using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class VisionFollower : NetworkBehaviour
{
    [Header("UI Elementos")]
    [Tooltip("Arraste o RectTransform do seu Vision Mask Image aqui.")]
    [SerializeField] private RectTransform visionMaskRect;

    [Header("Referências")]
    [Tooltip("Arraste o Canvas (o objeto pai da UI) aqui.")]
    [SerializeField] private Canvas canvas;

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
        if (debuffControl == null || !debuffControl.IsDebuffed())
        {
            return;
        }
        Vector3 worldPosition = transform.position;
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        Camera usedCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? mainCamera : null;
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