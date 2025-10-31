// LoadingScreenManager.cs
using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Gerencia a tela de loading durante a conexão e carregamento de cena no Netcode for GameObjects.
/// Este script NÃO é persistente e existe APENAS na LoadingScene.
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

    // NOVO CAMPO: Tempo mínimo que a tela de loading deve aparecer
    [Header("Configuração de Tempo")]
    [Tooltip("Tempo mínimo em segundos que a tela de loading deve ser exibida.")]
    [SerializeField] private float minDisplayTime = 3.0f; // Valor padrão de 3 segundos

    private bool isSceneLoaded = false;
    private bool arePlayersConnected = false;
    private Coroutine textAnimationCoroutine;
    private int currentDots = 0;

    // NOVO CAMPO: Para controlar o tempo de início
    private float timeStartedLoading;

    private void Awake()
    {
        // Garante que a tela de loading esteja visível ao iniciar a cena
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(true);
        }
    }

    private void Start()
    {
        // NOVO: Registra o tempo em que a cena de loading começou
        timeStartedLoading = Time.time;

        // FIX para NullReferenceException: Espera que o NetworkManager esteja pronto
        StartCoroutine(RegisterNetworkCallbacksWhenReady());

        // Inicia animação do texto
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
        textAnimationCoroutine = StartCoroutine(AnimateLoadingText());

        // Se o cliente já estiver conectado (vindo do menu), verifica o estado imediatamente
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            CheckIfAllPlayersConnected();
        }

        // Adiciona um listener para o carregamento da cena de jogo (que é carregada depois da LoadingScene)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        }
    }

    private IEnumerator RegisterNetworkCallbacksWhenReady()
    {
        // Espera até que o NetworkManager.Singleton não seja nulo
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        // Espera até que o NetworkManager esteja inicializado (IsListening)
        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // Registra callbacks do NetworkManager
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[LoadingScreen] Cliente conectado: {clientId}");
        CheckIfAllPlayersConnected();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[LoadingScreen] Cliente desconectado: {clientId}");
        arePlayersConnected = false;
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadMode)
    {
        // Se o cliente que acabou de carregar for o cliente local
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Verifica se a cena carregada não é a cena de loading (que já estamos nela)
            if (GameFlowManager.Instance != null && sceneName != GameFlowManager.Instance.loadingSceneName)
            {
                isSceneLoaded = true;
                CheckIfCanHideLoadingScreen();
            }
        }
    }

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
            int connectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;

            if (connectedPlayers >= expectedPlayerCount)
            {
                arePlayersConnected = true;
                Debug.Log($"[LoadingScreen] Todos os jogadores conectados ({connectedPlayers}/{expectedPlayerCount})");
                CheckIfCanHideLoadingScreen();
            }
        }
    }

    private void CheckIfCanHideLoadingScreen()
    {
        if (isSceneLoaded && arePlayersConnected)
        {
            Debug.Log("[LoadingScreen] Condições atendidas: escondendo loading screen");

            // NOVO: Calcula o tempo de espera necessário
            float timeSinceStart = Time.time - timeStartedLoading;
            float timeToWait = Mathf.Max(0f, minDisplayTime - timeSinceStart);

            StartCoroutine(HideLoadingScreenDelayed(timeToWait));
        }
    }

    private IEnumerator HideLoadingScreenDelayed(float delay)
    {
        // Se o tempo de espera for zero, ele espera apenas 1 frame para garantir que tudo foi renderizado
        if (delay <= 0)
        {
            yield return null;
        }
        else
        {
            yield return new WaitForSeconds(delay);
        }

        HideLoadingScreen();
    }
}
