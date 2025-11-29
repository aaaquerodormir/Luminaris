using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

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
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;

        NetworkManager.Singleton.SceneManager.OnLoadComplete += InitializeLevelOnLoad;

        InitializeLevelOnLoad(NetworkManager.Singleton.LocalClientId, SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null) return;
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= InitializeLevelOnLoad;
    }

    private void InitializeLevelOnLoad(ulong clientId, string sceneName, LoadSceneMode loadMode)
    {
        if (!IsServer) return;

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
        allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);

        if (allDoors.Length == 0)
        {
            return;
        }
        foreach (FinalDoor door in allDoors)
        {
            door.playerInside.OnValueChanged += OnDoorStateChanged;
        }
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

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            if (!door.IsPlayerInside)
            {
                allPlayersInside = false;
                break;
            }
        }

        if (allPlayersInside)
        {
            LoadNextScene();
        }
    }


    private void LoadNextScene()
    {
        if (!IsServer) return;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.TransitionToScene(nextSceneName);
        }
        else
        {        }
    }
}