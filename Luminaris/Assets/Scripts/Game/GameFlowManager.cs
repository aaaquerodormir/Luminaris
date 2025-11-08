using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Configuração de Cenas")]
    [Tooltip("O nome da cena que contém apenas a UI de Loading (sem câmera).")]
    public string loadingSceneName = "LoadingScene";

    [Header("Configuração de Tempo")]
    [Tooltip("Tempo mínimo em segundos que a tela de loading deve ser exibida.")]
    [SerializeField] private float loadingDuration = 5.0f;

    [Header("Configuração de Mensagens (Loading)")]
    [Tooltip("Adicione aqui todas as mensagens que podem aparecer na tela de loading.")]
    public string[] loadingMessages;

    public NetworkVariable<int> CurrentLoadingMessageIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private static int s_serverMessageIndex = -1;

    // Variável de rede para sincronizar o nome da próxima cena a ser carregada
    private NetworkVariable<Unity.Collections.FixedString32Bytes> nextSceneToLoad = new NetworkVariable<Unity.Collections.FixedString32Bytes>();

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

    public void TransitionToScene(string sceneName)
    {
        if (!IsServer || isTransitioning) return;

        Debug.Log($"[GameFlowManager] Iniciando transição para a cena: {sceneName}");
        isTransitioning = true;
        nextSceneToLoad.Value = sceneName;

        if (loadingMessages.Length > 0)
        {
            s_serverMessageIndex = (s_serverMessageIndex + 1) % loadingMessages.Length;
        }
        else
        {
            s_serverMessageIndex = 0; // Fallback
        }
        CurrentLoadingMessageIndex.Value = s_serverMessageIndex;
        Debug.Log($"[GameFlowManager] Definindo índice de loading para: {s_serverMessageIndex}");

        // 1. Carrega a cena de loading para todos os clientes
        NetworkManager.Singleton.SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // O evento OnLoadComplete é acionado em todos os clientes (incluindo o host) quando uma cena é carregada.
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
        // Se a cena que acabou de carregar foi a cena de loading...
        if (sceneName == loadingSceneName)
        {
            // O servidor agora inicia a Coroutine de espera
            if (IsServer)
            {
                // Inicia a rotina que vai esperar e depois carregar a cena do jogo
                StartCoroutine(LoadGameSceneAfterDelay());
            }
        }
        // Se a cena de destino (a cena do jogo) acabou de carregar...
        else if (sceneName == nextSceneToLoad.Value.ToString())
        {
            isTransitioning = false;
            Debug.Log($"[GameFlowManager] Transição completa. Cena '{sceneName}' carregada.");
        }
    }
    private IEnumerator LoadGameSceneAfterDelay()
    {
        Debug.Log($"[GameFlowManager] Cena de loading carregada. Esperando {loadingDuration} segundos...");

        // Espera o tempo definido em 'loadingDuration'
        yield return new WaitForSeconds(loadingDuration);

        // Após a espera, carrega a cena de destino
        Debug.Log($"[GameFlowManager] Tempo de espera terminado. Carregando a cena de destino: {nextSceneToLoad.Value}");
        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneToLoad.Value.ToString(), LoadSceneMode.Single);
    }
}


