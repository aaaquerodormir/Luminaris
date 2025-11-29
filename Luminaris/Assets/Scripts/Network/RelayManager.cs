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

    public async Task<string> CreateRelay(int maxPlayers = 2)
    {
        await EnsureInitialized();

        if (NetworkManager.Singleton == null)
        {
            return null;
        }

        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ut == null)
            {
                return null;
            }

            ut.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData
            );

            NetworkManager.Singleton.StartHost();


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

    public async Task<bool> JoinRelay(string joinCode)
    {
        await EnsureInitialized();

        if (NetworkManager.Singleton == null)
        {
            return false;
        }

        try
        {
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ut == null)
            {
                return false;
            }

            ut.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

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