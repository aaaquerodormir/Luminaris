using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq; // Adicionado para usar FindObjectsByType

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configura��o de N�vel")]
    [Tooltip("O NOME da cena para carregar quando todos os jogadores estiverem prontos")]
    [SerializeField] private string nextSceneName = "Fase2";
    public readonly NetworkVariable<int> PlayerDoorCount = new(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private FinalDoor[] allDoors;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Assumindo que este LevelManager � recriado em cada cena de jogo.
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return; // Apenas o servidor deve lidar com a busca de portas

        // Inscreve a inicializa��o para quando a cena for carregada
        NetworkManager.Singleton.SceneManager.OnLoadComplete += InitializeLevelOnLoad;

        // Para a primeira cena, executa a inicializa��o imediatamente (se n�o for a fase de lobby/load)
        InitializeLevelOnLoad(NetworkManager.Singleton.LocalClientId, SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null) return;

        // Cancela a inscri��o do handler de cena
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= InitializeLevelOnLoad;
    }

    private void InitializeLevelOnLoad(ulong clientId, string sceneName, LoadSceneMode loadMode)
    {
        if (!IsServer) return;

        // 1. Garante que qualquer inscri��o antiga seja limpa (se for o LevelManager persistente)
        if (allDoors != null)
        {
            foreach (FinalDoor door in allDoors)
            {
                if (door != null && door.playerInside != null)
                {
                    door.playerInside.OnValueChanged -= OnDoorStateChanged;
                }
            }
        }

        // 2. Encontra todas as NOVAS portas na cena rec�m-carregada
        allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);

        if (allDoors.Length == 0)
        {
            Debug.LogWarning($"[GameLevelManager-SERVER] Nenhuma 'FinalDoor' encontrada na cena {sceneName}.");
            return;
        }

        // 3. Inscreve o m�todo de checagem em cada porta da nova cena
        foreach (FinalDoor door in allDoors)
        {
            door.playerInside.OnValueChanged += OnDoorStateChanged;
        }

        Debug.Log($"[GameLevelManager-SERVER] N�vel '{sceneName}' inicializado. Monitorando {allDoors.Length} portas.");
    }

    private void OnDoorStateChanged(bool oldState, bool newState)
    {
        if (!IsServer) return;
        CheckForAllPlayersReady();
    }

    public void CheckForAllPlayersReady()
    {
        if (!IsServer) return;

        if (allDoors == null || allDoors.Length == 0)
        {
            allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
            if (allDoors.Length == 0) return;
        }

        Debug.Log("[GameLevelManager] Verificando se todos os jogadores est�o prontos (Reativo)...");

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            if (!door.IsPlayerInside)
            {
                allPlayersInside = false;
                break;
            }
        }

        Debug.Log($"[GameLevelManager] Todos os jogadores est�o na porta: {allPlayersInside}");

        if (allPlayersInside)
        {
            LoadNextScene();
        }
    }


    private void LoadNextScene()
    {
        if (!IsServer) return;

        Debug.Log($"[GameLevelManager] Solicitando transi��o para a pr�xima cena: {nextSceneName}");

        // NOVO: Usa o GameFlowManager para a transi��o de cena
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TransitionToScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[GameLevelManager] GameFlowManager.Instance � nulo! Verifique se o GameFlowManager est� na cena de Menu.");
        }
    }
}