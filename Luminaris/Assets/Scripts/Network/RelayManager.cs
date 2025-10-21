using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;


public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }
    private Task _initTask;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private async Task EnsureInitialized()
    {
        if (_initTask != null) { await _initTask; return; }

        _initTask = InitializeAsync();
        await _initTask;
    }

    private async Task InitializeAsync()
    {
        try
        {
            Debug.Log("[RelayManager] Inicializando Unity Services...");
            await UnityServices.InitializeAsync();
            Debug.Log("[RelayManager] Unity Services inicializado.");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("[RelayManager] Autenticando anonimamente...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[RelayManager] Autenticado. PlayerId: {AuthenticationService.Instance.PlayerId}");
            }
            else
            {
                Debug.Log("[RelayManager] Já autenticado.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayManager] Erro na inicialização/auth: {e}");
            throw;
        }
    }

    /// <summary>
    /// Cria uma allocation (host). Retorna joinCode ou null em falha.
    /// </summary>
    public async Task<string> CreateRelay(int maxPlayers = 2)
    {
        await EnsureInitialized();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayManager] NetworkManager.Singleton é null. Coloque um NetworkManager na cena (Menu).");
            return null;
        }

        try
        {
            Debug.Log("[RelayManager] Criando allocation Relay...");
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log($"[RelayManager] Allocation criada. joinCode={joinCode}");

            // Configura o UnityTransport para usar Relay.
            var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ut == null)
            {
                Debug.LogError("[RelayManager] UnityTransport não encontrado no NetworkManager.");
                return null;
            }

            // --- USO DO OVERLOAD CLÁSSICO (mais compatível com várias versões) ---
            // Se sua versão usa outro overload, adapte conforme a sua API.
            ut.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData
            );

            Debug.Log("[RelayManager] Transport configurado para Relay. Iniciando Host...");
            NetworkManager.Singleton.StartHost();

            // Opcional: LoadScene pelo NetworkManager (se quiser mudar de cena aqui)
            // NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", UnityEngine.SceneManagement.LoadSceneMode.Single);

            return joinCode;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"[RelayManager] RelayServiceException: {ex.Message}");
            return null;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RelayManager] Erro inesperado ao criar Relay: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Faz o client entrar na relay com joinCode.
    /// </summary>
    public async Task<bool> JoinRelay(string joinCode)
    {
        await EnsureInitialized();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[RelayManager] NetworkManager.Singleton é null. Coloque um NetworkManager na cena (Menu).");
            return false;
        }

        try
        {
            Debug.Log($"[RelayManager] Solicitando join com código: {joinCode}");
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ut == null)
            {
                Debug.LogError("[RelayManager] UnityTransport não encontrado no NetworkManager.");
                return false;
            }

            // Overload com hostConnectionData (= joinAlloc.HostConnectionData)
            ut.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            Debug.Log("[RelayManager] Transport configurado. Iniciando cliente...");
            NetworkManager.Singleton.StartClient();
            return true;
        }
        catch (RelayServiceException ex)
        {
            Debug.LogError($"[RelayManager] Erro ao entrar no Relay: {ex.Message}");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RelayManager] Erro inesperado ao entrar no Relay: {ex}");
            return false;
        }
    }
}