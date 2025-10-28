using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Configura��o de N�vel")]
    [Tooltip("O NOME da cena para carregar quando todos os jogadores estiverem prontos")]
    [SerializeField] private string nextSceneName = "Fase2";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CheckForAllPlayersReady()
    {

        if (!IsServer) return;

        Debug.Log("[GameLevelManager] Verificando se todos os jogadores est�o prontos...");

        FinalDoor[] allDoors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);

        if (allDoors.Length == 0)
        {
            Debug.LogWarning("[GameLevelManager] Nenhuma 'FinalDoor' encontrada na cena.");
            return;
        }

        bool allPlayersInside = true;
        foreach (FinalDoor door in allDoors)
        {
            // Verifica o .Value da NetworkVariable de cada porta
            if (!door.IsPlayerInside)
            {
                allPlayersInside = false;
                break; // Se um estiver fora, n�o precisa verificar os outros
            }
        }

        Debug.Log($"[GameLevelManager] Todos os jogadores est�o na porta: {allPlayersInside}");

        // Se todos estiverem dentro, carregue a pr�xima cena
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