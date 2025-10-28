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

    // Este método é chamado localmente pelo AttackClientRpc do Inimigo
    public void StartFade()
    {
        if (screenFadeAnimator != null)
        {
            screenFadeAnimator.SetTrigger("FadeIn");
        }
    }
}