using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Referências de UI Persistentes")]
    [Tooltip("Arraste o Canvas Principal aqui.")]
    public Canvas MainCanvas;

    [Header("Máscaras de Visão")]
    public Image VisionMaskImage_P1;
    public RectTransform VisionMaskRect_P1;

    public Image VisionMaskImage_P2;
    public RectTransform VisionMaskRect_P2;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (MainCanvas == null)
        {
            MainCanvas = GetComponent<Canvas>();
        }
    }
}