using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Configuração de Cenas")]
    public string loadingSceneName = "LoadingScene";
    [SerializeField] private string gameOverSceneName = "GameOverScene";

    [Header("Configuração de Tempo")]
    [SerializeField] private float loadingDuration = 5.0f;

    [Header("Configuração de Mensagens (Loading)")]
    public string[] loadingMessages;

    public NetworkVariable<int> CurrentLoadingMessageIndex = new NetworkVariable<int>();
    private static int s_serverMessageIndex = -1;

    private NetworkVariable<Unity.Collections.FixedString32Bytes> nextSceneToLoad = new NetworkVariable<Unity.Collections.FixedString32Bytes>();
    private string m_LastLoadedGameScene;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [ClientRpc]
    private void ForceTimeScaleClientRpc(float scale)
    {
        Time.timeScale = scale;
    }

    public void TransitionToScene(string sceneName, bool useLoadingScreen = true)
    {
        if (!IsServer || isTransitioning) return;

        ForceTimeScaleClientRpc(1.0f);
        isTransitioning = true;
        nextSceneToLoad.Value = sceneName;

        if (useLoadingScreen)
        {
            if (loadingMessages.Length > 0)
            {
                s_serverMessageIndex = (s_serverMessageIndex + 1) % loadingMessages.Length;
            }
            else
            {
                s_serverMessageIndex = 0;
            }
            CurrentLoadingMessageIndex.Value = s_serverMessageIndex;
            NetworkManager.Singleton.SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
        }
        else
        {
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnLoadComplete;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnLoadComplete;
        }
    }

    private void OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadMode)
    {
        if (sceneName == loadingSceneName)
        {
            if (IsServer)
            {
                StartCoroutine(LoadGameSceneAfterDelay());
            }
        }
        else if (sceneName == nextSceneToLoad.Value.ToString())
        {
            if (IsServer)
            {
                if (sceneName != loadingSceneName && sceneName != gameOverSceneName)
                {
                    m_LastLoadedGameScene = sceneName;
                }
            }
            isTransitioning = false;
        }
    }

    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(loadingDuration);

        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneToLoad.Value.ToString(), LoadSceneMode.Single);
    }

    public void RequestRetry()
    {
        RequestRetryServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRetryServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (string.IsNullOrEmpty(m_LastLoadedGameScene))
        {
            return;
        }
        TransitionToScene(m_LastLoadedGameScene, true);
    }
}