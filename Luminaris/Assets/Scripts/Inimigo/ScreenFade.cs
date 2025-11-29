using Unity.Netcode;
using UnityEngine;

public class ScreenFade : NetworkBehaviour
{
    public static ScreenFade Instance { get; private set; }

    [SerializeField]
    private Animator screenFadeAnimator;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void StartFade()
    {
        if (screenFadeAnimator != null)
        {
            screenFadeAnimator.SetTrigger("FadeIn");
        }
    }
}