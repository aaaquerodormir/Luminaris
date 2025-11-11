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

    // --- MUDANÇA AQUI ---
    // Adicionado o parâmetro 'useLoadingScreen'
    public void TransitionToScene(string sceneName, bool useLoadingScreen = true)
    {
        if (!IsServer || isTransitioning) return;

        ForceTimeScaleClientRpc(1.0f);
        isTransitioning = true;
        nextSceneToLoad.Value = sceneName; // Sempre definimos o destino

        if (useLoadingScreen)
        {
            // Comportamento antigo: Usar a tela de loading
            Debug.Log($"[GameFlowManager] Iniciando transição (COM LOADING) para: {sceneName}");

            if (loadingMessages.Length > 0)
            {
                s_serverMessageIndex = (s_serverMessageIndex + 1) % loadingMessages.Length;
            }
            else
            {
                s_serverMessageIndex = 0;
            }
            CurrentLoadingMessageIndex.Value = s_serverMessageIndex;

            // Carrega a cena de loading
            NetworkManager.Singleton.SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
        }
        else
        {
            // Comportamento novo: Pular a tela de loading
            Debug.Log($"[GameFlowManager] Iniciando transição (IMEDIATA) para: {sceneName}");

            // Carrega a cena de destino diretamente
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
        // Se a cena de loading acabou de carregar
        if (sceneName == loadingSceneName)
        {
            if (IsServer)
            {
                StartCoroutine(LoadGameSceneAfterDelay());
            }
        }
        // Se a cena de destino (jogo ou gameover) acabou de carregar
        else if (sceneName == nextSceneToLoad.Value.ToString())
        {
            if (IsServer)
            {
                // Só armazena o nome se for uma cena de JOGO
                if (sceneName != loadingSceneName && sceneName != gameOverSceneName)
                {
                    m_LastLoadedGameScene = sceneName;
                    Debug.Log($"[GameFlowManager] Cena de jogo '{m_LastLoadedGameScene}' armazenada para Retry.");
                }
            }
            isTransitioning = false;
        }
    }

    private IEnumerator LoadGameSceneAfterDelay()
    {
        yield return new WaitForSecondsRealtime(loadingDuration);

        Debug.Log($"[GameFlowManager] Carregando a cena de destino: {nextSceneToLoad.Value}");
        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneToLoad.Value.ToString(), LoadSceneMode.Single);
    }

    // --- LÓGICA DE RETRY ---
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
            Debug.LogError($"[GameFlowManager] 'm_LastLoadedGameScene' está vazia!");
            return;
        }

        // --- MUDANÇA AQUI ---
        // Ao reiniciar, QUEREMOS a tela de loading.
        // O 'true' é o padrão, então podemos omitir ou ser explícitos.
        TransitionToScene(m_LastLoadedGameScene, true);
    }
}