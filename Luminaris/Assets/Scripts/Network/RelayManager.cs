using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;

public class RelayManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject painelMultiplayer;
    [SerializeField] private TMP_Text codigoSalaText;
    [SerializeField] private TMP_InputField inputJoinCode;

    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private int maxPlayers = 2;

    private void Start()
    {
        if (painelMultiplayer != null)
            painelMultiplayer.SetActive(false);
    }

    public void AbrirPainel()
    {
        painelMultiplayer.SetActive(true);
        codigoSalaText.text = "";
    }

    public void FecharPainel()
    {
        painelMultiplayer.SetActive(false);
    }

    public async void HostGame()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager não encontrado.");
            return;
        }

        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            codigoSalaText.text = $"Código: {joinCode}";
            GUIUtility.systemCopyBuffer = joinCode;
            Debug.Log($"[Relay] Host criado com código: {joinCode}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                alloc.RelayServer.IpV4,
                (ushort)alloc.RelayServer.Port,
                alloc.AllocationIdBytes,
                alloc.Key,
                alloc.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Erro ao criar Host: {e}");
        }
    }

    public async void JoinGame()
    {
        string joinCode = inputJoinCode.text.Trim();
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("Código de sala vazio!");
            return;
        }

        try
        {
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log("[Relay] Entrando na partida...");
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"[Relay] Erro ao conectar: {e}");
        }
    }
}