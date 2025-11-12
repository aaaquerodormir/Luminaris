using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public static event System.Action OnGameOver;

    [Header("Lava")]
    [SerializeField] private LavaRise lava;

    [Header("Controle de Turnos")]
    [SerializeField] private TurnControl turnControl;

    [Header("Controle de Cenas")]
    [Tooltip("O nome da cena de GameOver para onde o jogo deve transicionar.")]
    [SerializeField] private string gameOverSceneName = "GameOverScene";

    [Header("UI Geral")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private GameObject hudContainer;
    [SerializeField] private GameObject confirmationUI;

    [Header("Transições")]
    [Tooltip("Painel de Imagem para cobrir a tela durante as transições")]
    [SerializeField] private GameObject faderPanel;

    [Header("Configurações de Fade")]
    [Tooltip("Duração do fade out em segundos (padrão: 0.5s)")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("Duração do fade in em segundos (padrão: 0.5s)")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("UI Específica")]
    [SerializeField] private GameObject jumpCounterUI;

    private bool isGameOver = false;
    private GameSession session;
    private Checkpoint lastCheckpoint;
    private System.Action confirmedAction;

    // Referência ao componente Image do faderPanel
    private Image faderImage;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            session = new GameSession();
        }
        else
        {
            Destroy(gameObject);
        }

        // Inicializa o faderPanel
        InitializeFader();
    }

    private void Start()
    {
        SetFadeAlpha(0f);
    }

    private void OnEnable()
    {
        PlayerRespawn.OnPlayerDied += OnAnyPlayerDeath;
    }

    private void OnDisable()
    {
        PlayerRespawn.OnPlayerDied -= OnAnyPlayerDeath;
    }

    private void InitializeFader()
    {
        if (faderPanel == null)
        {
            Debug.LogWarning("[GameManager] faderPanel não está atribuído no Inspector!");
            return;
        }

        // Pega o componente Image do faderPanel
        faderImage = faderPanel.GetComponent<Image>();

        if (faderImage == null)
        {
            Debug.LogError("[GameManager] faderPanel não tem um componente Image! Adicione um componente Image ao GameObject.");
            return;
        }

        // Garante que o faderPanel está ativo
        faderPanel.SetActive(true);

        // Garante que o Canvas está na frente de tudo
        Canvas canvas = faderPanel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 9999;
        }
    }

    private IEnumerator FadeIn()
    {
        if (faderImage == null) yield break;

        float elapsedTime = 0f;
        Color color = faderImage.color;
        float startAlpha = color.a;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;

            float alpha = Mathf.SmoothStep(startAlpha, 0f, normalizedTime);

            color.a = alpha;
            faderImage.color = color;

            yield return null;
        }

        color.a = 0f;
        faderImage.color = color;
    }

    private IEnumerator FadeOut()
    {
        if (faderImage == null) yield break;

        // Garante que o faderPanel está ativo
        if (!faderPanel.activeSelf)
        {
            faderPanel.SetActive(true);
        }

        float elapsedTime = 0f;
        Color color = faderImage.color;
        float startAlpha = color.a;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / fadeOutDuration;

            float alpha = Mathf.SmoothStep(startAlpha, 1f, normalizedTime);

            color.a = alpha;
            faderImage.color = color;

            yield return null;
        }

        color.a = 1f;
        faderImage.color = color;
    }

    // --- MORTE GLOBAL ---
    private void OnAnyPlayerDeath()
    {
        if (!IsServer || isGameOver) return;
        isGameOver = true;

        PauseGameAndEnableTransitionSafeguardsClientRpc();

        StartCoroutine(DelayedSceneTransition());
    }

    private IEnumerator DelayedSceneTransition()
    {
        // Aguarda o fade out completar antes de trocar de cena
        yield return StartCoroutine(FadeOut());

        // Pequeno delay adicional para garantir que o RPC foi processado
        yield return new WaitForSeconds(0.1f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TransitionToScene(gameOverSceneName, false);
        }
        else
        {
            Debug.LogError("[GameManager] GameFlowManager.Instance é nulo!");
        }
    }


    [ClientRpc]
    private void PauseGameAndEnableTransitionSafeguardsClientRpc()
    {
        // Limpa a UI
        if (pauseMenu != null) pauseMenu.gameObject.SetActive(false);
        if (hudContainer != null) hudContainer.SetActive(false);

        if (TurnControl.Instance != null)
            TurnControl.Instance.HideAllTurnUI();

        // Inicia o fade out no cliente
        StartCoroutine(FadeOut());

        OnGameOver?.Invoke();
    }
    [ServerRpc(RequireOwnership = false)]
    public void InvokeGameOverServerRpc()
    {
        if (!IsServer) return;
        OnAnyPlayerDeath();
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerDiedServerRpc()
    {
        if (!IsServer) return;
        OnAnyPlayerDeath();
    }
    public void StartFadeIn()
    {
        StartCoroutine(FadeIn());
    }

    public void StartFadeOut()
    {
        StartCoroutine(FadeOut());
    }
    public void SetFadeAlpha(float alpha)
    {
        if (faderImage != null)
        {
            Color color = faderImage.color;
            color.a = Mathf.Clamp01(alpha);
            faderImage.color = color;
        }
    }
}
