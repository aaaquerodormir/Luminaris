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

    // Essas variáveis agora serão lidas no Update()
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
        StartCoroutine(RegisterNetworkCallbacksWhenReady());
        StartCoroutine(SetLoadingMessage());

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            CheckIfAllPlayersConnected();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        }
    }

    // --- MUDANÇA 1: Adição do Update() ---
    // Este método verifica constantemente o estado do carregamento.
    private void Update()
    {
        // Verifica se a cena foi carregada E se os players estão conectados
        if (isSceneLoaded && arePlayersConnected)
        {
            // Se ambas as condições forem verdadeiras, esconde a tela de loading.
            HideLoadingScreen();
        }
    }

    // --- MUDANÇA 2: Novo Método ---
    // Colocamos a lógica de "esconder" em um método separado.
    private void HideLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
            Debug.Log("[LoadingScreen] Carregamento completo. Escondendo tela.");
        }

        // Desativa este script para que o Update() pare de rodar
        // e consumir performance.
        this.enabled = false;
    }

    private IEnumerator SetLoadingMessage()
    {
        while (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[LoadingScreen] Esperando GameFlowManager.Instance...");
            yield return null;
        }

        GameFlowManager flow = GameFlowManager.Instance;

        while (flow.loadingMessages == null || flow.loadingMessages.Length == 0)
        {
            Debug.LogWarning("[LoadingScreen] Esperando GameFlowManager carregar a lista de mensagens...");
            yield return new WaitForSeconds(0.1f);
        }

        int messageIndex = flow.CurrentLoadingMessageIndex.Value;
        string[] messages = flow.loadingMessages;

        if (messageIndex >= 0 && messageIndex < messages.Length)
        {
            messageText.text = messages[messageIndex];
            Debug.Log($"[LoadingScreen] Exibindo mensagem {messageIndex}: {messages[messageIndex]}");
        }
        else
        {
            Debug.LogError($"[LoadingScreen] Índice de mensagem inválido ({messageIndex}) recebido.");
            messageText.text = "Luma e Luna precisam da sua ajuda...";
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
            }
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private IEnumerator RegisterNetworkCallbacksWhenReady()
    {
        while (NetworkManager.Singleton == null)
        {
            yield return null;
        }

        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

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
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
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