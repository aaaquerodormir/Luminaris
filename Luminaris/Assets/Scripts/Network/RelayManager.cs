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

    // --- MÉTODOS ORIGINAIS DO RELAY (INTERNET) ---
    // (Todo o seu código original de Relay permanece aqui, sem alterações)
    #region Relay Methods

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
            Debug.LogError("[RelayManager] NetworkManager.Singleton é null.");
            return null;
        }

        try
        {
            Debug.Log("[RelayManager] Criando allocation Relay...");
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log($"[RelayManager] Allocation criada. joinCode={joinCode}");

            var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (ut == null)
            {
                Debug.LogError("[RelayManager] UnityTransport não encontrado no NetworkManager.");
                return null;
            }

            ut.SetRelayServerData(new RelayServerData(alloc, "dtls")); // Usando o construtor moderno

            Debug.Log("[RelayManager] Transport configurado para Relay. Iniciando Host...");
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
            Debug.LogError("[RelayManager] NetworkManager.Singleton é null.");
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

            ut.SetRelayServerData(new RelayServerData(joinAlloc, "dtls")); // Usando o construtor moderno

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
    #endregion

    // --- NOVOS MÉTODOS PARA LAN (REDE LOCAL) ---
    #region LAN Methods

    /// <summary>
    /// Inicia o jogo como um Host na rede local (LAN).
    /// </summary>
    public void StartLanHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[LAN] NetworkManager.Singleton é null.");
            return;
        }

        var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (ut == null)
        {
            Debug.LogError("[LAN] UnityTransport não encontrado no NetworkManager.");
            return;
        }

        ut.SetConnectionData("0.0.0.0", 5000);
        Debug.Log("[LAN] Iniciando Host na rede local...");
        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Entra em um jogo como Client na rede local (LAN), conectando a um IP.
    /// </summary>
    public void JoinLanClient(string ipAddress)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[LAN] NetworkManager.Singleton é null.");
            return;
        }

        var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (ut == null)
        {
            Debug.LogError("[LAN] UnityTransport não encontrado no NetworkManager.");
            return;
        }

        ut.SetConnectionData(ipAddress, 5000);
        Debug.Log($"[LAN] Conectando ao Host no IP: {ipAddress}...");
        NetworkManager.Singleton.StartClient();
    }

    #endregion
}