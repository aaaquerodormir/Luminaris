using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class VisionFollower : NetworkBehaviour
{
    private RectTransform visionMaskRect;
    private Image visionMaskImage;
    private Canvas canvas;
    private Camera localPlayerCamera;
    private DebuffVisionControl debuffControl;

    [Header("Limitação da Borda")]
    [SerializeField]
    private float screenEdgeBuffer = 10f;

    public override void OnNetworkSpawn()
    {

        localPlayerCamera = GetComponentInChildren<Camera>(true);
        debuffControl = GetComponent<DebuffVisionControl>();

        if (UIManager.Instance == null)
        {
            enabled = false;
            return;
        }

        canvas = UIManager.Instance.MainCanvas;

        if (OwnerClientId == 0)
        {
            visionMaskRect = UIManager.Instance.VisionMaskRect_P1;
            visionMaskImage = UIManager.Instance.VisionMaskImage_P1;
        }
        else
        {
            visionMaskRect = UIManager.Instance.VisionMaskRect_P2;
            visionMaskImage = UIManager.Instance.VisionMaskImage_P2;
        }

        if (debuffControl != null)
        {
            debuffControl.SetLocalMask(visionMaskImage);
        }
        else
        {
        }

        if (localPlayerCamera == null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (localPlayerCamera == null)
        {
            return;
        }

        if (debuffControl == null || !debuffControl.IsDebuffed() || visionMaskRect == null || canvas == null)
        {
            return;
        }

        Vector3 worldPosition = transform.position;
        Vector3 screenPoint = localPlayerCamera.WorldToScreenPoint(worldPosition);

        float viewportWidth = localPlayerCamera.pixelWidth;
        float viewportHeight = localPlayerCamera.pixelHeight;
        float maskHalfSize = Mathf.Min(visionMaskRect.rect.width, visionMaskRect.rect.height) / 2f;
        float buffer = maskHalfSize + screenEdgeBuffer;
        screenPoint.x = Mathf.Clamp(screenPoint.x, buffer, viewportWidth - buffer);
        screenPoint.y = Mathf.Clamp(screenPoint.y, buffer, viewportHeight - buffer);

        Camera usedCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? localPlayerCamera : null;

        if (usedCamera == null)
        {
            float viewportOffsetX = localPlayerCamera.rect.x * Screen.width;
            float viewportOffsetY = localPlayerCamera.rect.y * Screen.height;

            screenPoint.x += viewportOffsetX;
            screenPoint.y += viewportOffsetY;
        }

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