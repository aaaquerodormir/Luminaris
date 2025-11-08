using UnityEngine;
using Unity.Netcode;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("Loading Screen Components")]
    [SerializeField] private GameObject loadingScreenPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Animator backgroundAnimator;

    [Header("Scene Loading")]
    [SerializeField] private bool waitForAllPlayersConnected = true;
    [SerializeField] private int expectedPlayerCount = 2;

    private bool isSceneLoaded = false;
    private bool arePlayersConnected = false;


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
        // FIX para NullReferenceException: Espera que o NetworkManager esteja pronto
        StartCoroutine(RegisterNetworkCallbacksWhenReady());

        StartCoroutine(SetLoadingMessage());

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
    private IEnumerator SetLoadingMessage()
    {
        // Espera pelo GameFlowManager estar pronto
        while (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[LoadingScreen] Esperando GameFlowManager.Instance...");
            yield return null;
        }

        GameFlowManager flow = GameFlowManager.Instance;

        // Espera pelas mensagens (em caso de lag do cliente)
        while (flow.loadingMessages == null || flow.loadingMessages.Length == 0)
        {
            Debug.LogWarning("[LoadingScreen] Esperando GameFlowManager carregar a lista de mensagens...");
            yield return new WaitForSeconds(0.1f);
        }

        // Pega o índice SINCRONIZADO e o array de mensagens
        int messageIndex = flow.CurrentLoadingMessageIndex.Value;
        string[] messages = flow.loadingMessages;

        if (messageIndex >= 0 && messageIndex < messages.Length)
        {
            messageText.text = messages[messageIndex];
            Debug.Log($"[LoadingScreen] Exibindo mensagem {messageIndex}: {messages[messageIndex]}");
        }
        else
        {
            // Fallback caso algo dê muito errado
            Debug.LogError($"[LoadingScreen] Índice de mensagem inválido ({messageIndex}) recebido.");
            messageText.text = "Luma e Luna precisam da sua ajuda...";
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
            }
        }
    }

    private void CheckIfAllPlayersConnected()
    {
        if (!waitForAllPlayersConnected)
        {
            arePlayersConnected = true;
            return;
        }

        if (NetworkManager.Singleton != null)
        {
            int connectedPlayers = NetworkManager.Singleton.ConnectedClients.Count;

            if (connectedPlayers >= expectedPlayerCount)
            {
                arePlayersConnected = true;
                Debug.Log($"[LoadingScreen] Todos os jogadores conectados ({connectedPlayers}/{expectedPlayerCount})");
            }
        }
    }
}
