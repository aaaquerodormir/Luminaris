using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // O Singleton
    public static UIManager Instance { get; private set; }

    [Header("Referências de UI Persistentes")]
    [Tooltip("Arraste o Canvas Principal aqui.")]
    public Canvas MainCanvas;

    [Tooltip("Arraste o Image do Vision Mask aqui.")]
    public Image VisionMaskImage;

    [Tooltip("Arraste o RectTransform do Vision Mask aqui.")]
    public RectTransform VisionMaskRect;

    private void Awake()
    {
        // Garante que apenas uma instância exista
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Se este objeto for o Canvas, atribua a si mesmo
        if (MainCanvas == null)
        {
            MainCanvas = GetComponent<Canvas>();
        }
    }
}