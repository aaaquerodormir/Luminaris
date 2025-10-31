using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;

/// <summary>
/// Gerencia a tela de loading durante a conexão e carregamento de cena no Netcode for GameObjects
/// Adaptado para integrar com RelayManager, MainMenu e CustomPlayerSpawner
/// </summary>
public class LoadingScreenManager : MonoBehaviour
{
    [Header("Loading Screen Components")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Animator backgroundAnimator;

    [Header("Loading Text Animation")]
    [SerializeField] private string baseLoadingText = "Carregando";
    [SerializeField] private float dotAnimationSpeed = 0.5f;
    [SerializeField] private int maxDots = 3;

    [Header("Scene Loading")]
    [SerializeField] private bool waitForAllPlayersConnected = true;
    [SerializeField] private int expectedPlayerCount = 2;

    private bool isSceneLoaded = false;
    private bool arePlayersConnected = false;
    private int currentDots = 0;
    private Coroutine textAnimationCoroutine;

    private static LoadingScreenManager instance;

    public static LoadingScreenManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<LoadingScreenManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Inicialmente esconde a tela de loading
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Registra callbacks do NetworkManager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    private void OnDestroy()
    {
        // Remove callbacks ao destruir
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
            }
        }
    }

    /// <summary>
    /// Mostra a tela de loading
    /// </summary>
    public void ShowLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
            isSceneLoaded = false;
            arePlayersConnected = false;

            // Inicia animação do texto
            if (textAnimationCoroutine != null)
            {
                StopCoroutine(textAnimationCoroutine);
            }
            textAnimationCoroutine = StartCoroutine(AnimateLoadingText());

            Debug.Log("[LoadingScreen] Tela de loading mostrada");
        }
    }

    /// <summary>
    /// Esconde a tela de loading
    /// </summary>
    public void HideLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);

            // Para animação do texto
            if (textAnimationCoroutine != null)
            {
                StopCoroutine(textAnimationCoroutine);
                textAnimationCoroutine = null;
            }

            Debug.Log("[LoadingScreen] Tela de loading escondida");
        }
    }

    /// <summary>
    /// Anima o texto de loading com pontos
    /// </summary>
    private IEnumerator AnimateLoadingText()
    {
        while (true)
        {
            currentDots = (currentDots + 1) % (maxDots + 1);

            if (loadingText != null)
            {
                string dots = new string('.', currentDots);
                loadingText.text = baseLoadingText + dots;
            }

            yield return new WaitForSeconds(dotAnimationSpeed);
        }
    }

    /// <summary>
    /// Callback quando um cliente se conecta
    /// </summary>
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[LoadingScreen] Cliente conectado: {clientId}");

        // Se for o próprio cliente conectando, mostra a tela de loading
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            ShowLoadingScreen();
        }

        // Verifica se todos os jogadores esperados estão conectados
        CheckIfAllPlayersConnected();
    }

    /// <summary>
    /// Callback quando um cliente se desconecta
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[LoadingScreen] Cliente desconectado: {clientId}");
        arePlayersConnected = false;
    }

    /// <summary>
    /// Callback quando o carregamento da cena é completado
    /// </summary>
    private void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        Debug.Log($"[LoadingScreen] Cena carregada: {sceneName}");

        // Verifica se é uma das cenas de jogo (não o menu)
        if (sceneName == "SampleScene" || sceneName == "Fase2Final" || sceneName == "Fase3")
        {
            isSceneLoaded = true;
            CheckIfCanHideLoadingScreen();
        }
    }

    /// <summary>
    /// Verifica se todos os jogadores esperados estão conectados
    /// </summary>
    private void CheckIfAllPlayersConnected()
    {
        if (!waitForAllPlayersConnected)
        {
            arePlayersConnected = true;
            CheckIfCanHideLoadingScreen();
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            int connectedPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

            if (connectedPlayers >= expectedPlayerCount)
            {
                arePlayersConnected = true;
                Debug.Log($"[LoadingScreen] Todos os jogadores conectados ({connectedPlayers}/{expectedPlayerCount})");
                CheckIfCanHideLoadingScreen();
            }
        }
    }

    /// <summary>
    /// Verifica se pode esconder a tela de loading (cena carregada + jogadores conectados)
    /// </summary>
    private void CheckIfCanHideLoadingScreen()
    {
        if (isSceneLoaded && arePlayersConnected)
        {
            Debug.Log("[LoadingScreen] Condições atendidas: escondendo loading screen");
            // Aguarda um pouco para garantir que tudo está sincronizado
            StartCoroutine(HideLoadingScreenDelayed(1.5f));
        }
    }

    /// <summary>
    /// Esconde a tela de loading com delay
    /// </summary>
    private IEnumerator HideLoadingScreenDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideLoadingScreen();
    }

    /// <summary>
    /// Define o número esperado de jogadores
    /// </summary>
    public void SetExpectedPlayerCount(int count)
    {
        expectedPlayerCount = count;
    }

    /// <summary>
    /// Atualiza o texto de loading manualmente
    /// </summary>
    public void SetLoadingText(string text)
    {
        baseLoadingText = text;
    }

    /// <summary>
    /// Reseta o estado do loading (útil ao voltar ao menu)
    /// </summary>
    public void ResetLoadingState()
    {
        isSceneLoaded = false;
        arePlayersConnected = false;
        HideLoadingScreen();
    }
}
