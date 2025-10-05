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
    public static RelayManager Instance;
    public string JoinCode { get; private set; }

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

    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("[RelayManager] Unity Services inicializado com sucesso!");

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("[RelayManager] Autenticado anonimamente com sucesso.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RelayManager] Erro ao inicializar Unity Services: {e.Message}");
        }
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Debug.Log("[RelayManager] Criando Allocation...");
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"[RelayManager] Relay criado com código: {JoinCode}");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.StartHost();
            Debug.Log("[RelayManager] Host iniciado com sucesso!");
            return JoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] Erro ao criar Relay: {e.Message}");
            return null;
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"[RelayManager] Tentando entrar com código: {joinCode}");
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.StartClient();
            Debug.Log("[RelayManager] Cliente conectado com sucesso!");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[RelayManager] Erro ao entrar no Relay: {e.Message}");
        }
    }
}