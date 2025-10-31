using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Gerenciador persistente que orquestra todas as transi��es de cena no jogo multiplayer.
/// Usa uma cena de loading intermedi�ria para mascarar a transi��o.
/// </summary>
public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Configura��o de Cenas")]
    [Tooltip("O nome da cena que cont�m apenas a UI de Loading (sem c�mera).")]
    // CORRE��O: Alterado de [SerializeField] private string para public string para permitir acesso pelo LoadingScreenManager
    public string loadingSceneName = "LoadingScene";

    // Vari�vel de rede para sincronizar o nome da pr�xima cena a ser carregada
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
        // Torna o manager persistente
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Inicia a transi��o para uma nova cena. Chamado pelo Servidor.
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        if (!IsServer || isTransitioning) return;

        Debug.Log($"[GameFlowManager] Iniciando transi��o para a cena: {sceneName}");
        isTransitioning = true;
        nextSceneToLoad.Value = sceneName;

        // 1. Carrega a cena de loading para todos os clientes
        NetworkManager.Singleton.SceneManager.LoadScene(loadingSceneName, LoadSceneMode.Single);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // O evento OnLoadComplete � acionado em todos os clientes (incluindo o host) quando uma cena � carregada.
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
            // O servidor agora carrega a cena de destino (a cena do jogo)
            if (IsServer)
            {
                Debug.Log($"[GameFlowManager] Cena de loading carregada. Agora carregando a cena de destino: {nextSceneToLoad.Value}");
                NetworkManager.Singleton.SceneManager.LoadScene(nextSceneToLoad.Value.ToString(), LoadSceneMode.Single);
            }
            // O cliente (n�o-host) apenas espera a pr�xima cena ser carregada pelo servidor.
        }
        // Se a cena de destino (a cena do jogo) acabou de carregar...
        else if (sceneName == nextSceneToLoad.Value.ToString())
        {
            // A transi��o terminou.
            isTransitioning = false;
            Debug.Log($"[GameFlowManager] Transi��o completa. Cena '{sceneName}' carregada.");
        }
    }
}


