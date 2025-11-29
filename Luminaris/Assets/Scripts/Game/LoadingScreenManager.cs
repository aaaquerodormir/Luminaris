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

    private void Update()
    {
        if (isSceneLoaded && arePlayersConnected)
        {
            HideLoadingScreen();
        }
    }

    private void HideLoadingScreen()
    {
        if (loadingScreenPanel != null)
        {
            loadingScreenPanel.SetActive(false);
        }

        this.enabled = false;
    }

    private IEnumerator SetLoadingMessage()
    {
        while (GameFlowManager.Instance == null)
        {
            yield return null;
        }

        GameFlowManager flow = GameFlowManager.Instance;

        while (flow.loadingMessages == null || flow.loadingMessages.Length == 0)
        {
            yield return new WaitForSeconds(0.1f);
        }

        int messageIndex = flow.CurrentLoadingMessageIndex.Value;
        string[] messages = flow.loadingMessages;

        if (messageIndex >= 0 && messageIndex < messages.Length)
        {
            messageText.text = messages[messageIndex];
        }
        else
        {
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
        CheckIfAllPlayersConnected();
    }

    private void OnClientDisconnected(ulong clientId)
    {
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
            }
        }
    }
}