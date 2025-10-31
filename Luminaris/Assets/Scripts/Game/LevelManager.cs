using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq; // Adicionado para usar FindObjectsByType

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configuração de Nível")]
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
        // Assumindo que este LevelManager é recriado em cada cena de jogo.
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return; // Apenas o servidor deve lidar com a busca de portas

        // Inscreve a inicialização para quando a cena for carregada
        NetworkManager.Singleton.SceneManager.OnLoadComplete += InitializeLevelOnLoad;

        // Para a primeira cena, executa a inicialização imediatamente (se não for a fase de lobby/load)
        InitializeLevelOnLoad(NetworkManager.Singleton.LocalClientId, SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null) return;

        // Cancela a inscrição do handler de cena
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= InitializeLevelOnLoad;
    }

    private void InitializeLevelOnLoad(ulong clientId, string sceneName, LoadSceneMode loadMode)
    {
        if (!IsServer) return;

        // 1. Garante que qualquer inscrição antiga seja limpa (se for o LevelManager persistente)
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

        // 2. Encontra todas as NOVAS portas na cena recém-carregada
        allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);

        if (allDoors.Length == 0)
        {
            Debug.LogWarning($"[GameLevelManager-SERVER] Nenhuma 'FinalDoor' encontrada na cena {sceneName}.");
            return;
        }

        // 3. Inscreve o método de checagem em cada porta da nova cena
        foreach (FinalDoor door in allDoors)
        {
            door.playerInside.OnValueChanged += OnDoorStateChanged;
        }

        Debug.Log($"[GameLevelManager-SERVER] Nível '{sceneName}' inicializado. Monitorando {allDoors.Length} portas.");
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

        Debug.Log("[GameLevelManager] Verificando se todos os jogadores estão prontos (Reativo)...");

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            if (!door.IsPlayerInside)
            {
                allPlayersInside = false;
                break;
            }
        }

        Debug.Log($"[GameLevelManager] Todos os jogadores estão na porta: {allPlayersInside}");

        if (allPlayersInside)
        {
            LoadNextScene();
        }
    }


    private void LoadNextScene()
    {
        if (!IsServer) return;

        Debug.Log($"[GameLevelManager] Solicitando transição para a próxima cena: {nextSceneName}");

        // NOVO: Usa o GameFlowManager para a transição de cena
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TransitionToScene(nextSceneName);
        }
        else
        {
            Debug.LogError("[GameLevelManager] GameFlowManager.Instance é nulo! Verifique se o GameFlowManager está na cena de Menu.");
        }
    }
}