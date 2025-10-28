using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configura��o de N�vel")]
    [Tooltip("O NOME da cena para carregar quando todos os jogadores estiverem prontos")]
    [SerializeField] private string nextSceneName = "Fase2";
    public readonly NetworkVariable<int> PlayerDoorCount = new(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private FinalDoor[] allDoors;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return; // Apenas o servidor deve lidar com a busca de portas

        // Inscreve a inicializa��o para quando a cena for carregada
        NetworkManager.Singleton.SceneManager.OnLoadComplete += InitializeLevelOnLoad;

        // Para a primeira cena, executa a inicializa��o imediatamente (se n�o for a fase de lobby/load)
        // O NetworkManager pode n�o estar pronto, mas � mais seguro usar o evento acima.
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (!IsServer || NetworkManager.Singleton == null || NetworkManager.Singleton.SceneManager == null) return;

        // Cancela a inscri��o do handler de cena
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= InitializeLevelOnLoad;

        // ... (Manter a l�gica de cancelamento de inscri��o das portas, se necess�rio)
        // ... (Sua l�gica de OnDestroy para as portas, que j� est� OK)
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
            // **IMPORTANTE:** O Spawner precisa rodar depois disso para chamar door.SetTargetClientId(clientId)
        }

        Debug.Log($"[GameLevelManager-SERVER] N�vel '{sceneName}' inicializado. Monitorando {allDoors.Length} portas.");
    }

    /// <summary>
    /// O m�todo de checagem que agora reage �s mudan�as de NetworkVariable.
    /// </summary>
    private void OnDoorStateChanged(bool oldState, bool newState)
    {
        // Este m�todo ser� chamado APENAS no Servidor/Host sempre que uma NetworkVariable for alterada.
        if (!IsServer) return;

        CheckForAllPlayersReady();
    }

    public void CheckForAllPlayersReady()
    {
        if (!IsServer) return;

        // Se a lista de portas n�o foi populada (deveria ter sido em OnNetworkSpawn), tenta novamente.
        if (allDoors == null || allDoors.Length == 0)
        {
            allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
            if (allDoors.Length == 0) return;
        }

        Debug.Log("[GameLevelManager] Verificando se todos os jogadores est�o prontos (Reativo)...");

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            // O acesso � NetworkVariable (door.IsPlayerInside) agora reflete o 
            // valor J� sincronizado pelo Netcode antes desta fun��o ser chamada.
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
        // Garante que s� o servidor execute (novamente, por seguran�a)
        if (!IsServer) return;

        Debug.Log($"[GameLevelManager] Carregando pr�xima cena: {nextSceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(nextSceneName, LoadSceneMode.Single);
    }
}