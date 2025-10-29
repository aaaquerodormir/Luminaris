using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configuração de Nível")]
    [Tooltip("O NOME da cena para carregar quando todos os jogadores estiverem prontos")]
    [SerializeField] private string nextSceneName = "Fase2";
    public readonly NetworkVariable<int> PlayerDoorCount = new(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private FinalDoor[] allDoors;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return; // Apenas o servidor deve lidar com a busca de portas

        // Inscreve a inicialização para quando a cena for carregada
        NetworkManager.Singleton.SceneManager.OnLoadComplete += InitializeLevelOnLoad;

        // Para a primeira cena, executa a inicialização imediatamente (se não for a fase de lobby/load)
        // O NetworkManager pode não estar pronto, mas é mais seguro usar o evento acima.
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null) return;

        // Cancela a inscrição do handler de cena
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= InitializeLevelOnLoad;

        // ... (Manter a lógica de cancelamento de inscrição das portas, se necessário)
        // ... (Sua lógica de OnDestroy para as portas, que já está OK)
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
            // **IMPORTANTE:** O Spawner precisa rodar depois disso para chamar door.SetTargetClientId(clientId)
        }

        Debug.Log($"[GameLevelManager-SERVER] Nível '{sceneName}' inicializado. Monitorando {allDoors.Length} portas.");
    }

    /// <summary>
    /// O método de checagem que agora reage às mudanças de NetworkVariable.
    /// </summary>
    private void OnDoorStateChanged(bool oldState, bool newState)
    {
        // Este método será chamado APENAS no Servidor/Host sempre que uma NetworkVariable for alterada.
        if (!IsServer) return;

        CheckForAllPlayersReady();
    }

    public void CheckForAllPlayersReady()
    {
        if (!IsServer) return;

        // Se a lista de portas não foi populada (deveria ter sido em OnNetworkSpawn), tenta novamente.
        if (allDoors == null || allDoors.Length == 0)
        {
            allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
            if (allDoors.Length == 0) return;
        }

        Debug.Log("[GameLevelManager] Verificando se todos os jogadores estão prontos (Reativo)...");

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            // O acesso à NetworkVariable (door.IsPlayerInside) agora reflete o 
            // valor JÁ sincronizado pelo Netcode antes desta função ser chamada.
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
        // Garante que só o servidor execute (novamente, por segurança)
        if (!IsServer) return;

        Debug.Log($"[GameLevelManager] Carregando próxima cena: {nextSceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}