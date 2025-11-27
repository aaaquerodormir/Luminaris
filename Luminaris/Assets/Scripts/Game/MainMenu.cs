using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using System.Net;
using System.Linq;

public class MainMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject painelMultiplayer;
    [SerializeField] private GameObject painelLan;
    [SerializeField] private GameObject painelModo;

    [Header("Configurações")]
    [SerializeField] private string firstGameSceneName = "Fase1";
    // --- ADICIONADO: A REFERÊNCIA SIMPLES QUE VOCÊ PEDIU ---
    [SerializeField] private TMP_Text textoStatus;
    // -------------------------------------------------------

    [Header("Relay UI")]
    [SerializeField] private TMP_Text relayCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("LAN UI")]
    [SerializeField] private TMP_InputField ipInputLan;
    [SerializeField] private TMP_Text hostIpDisplay;
    [SerializeField] private int lanPort = 7777;

    private RelayManager relayManager;
    private UnityTransport transport;
    private NetworkManager netManager;

    private bool _isConnecting = false;
    private bool _lanSceneLoaded = false;

    private GameFlowManager gameFlowManager;

    private void Start()
    {
        MostrarPrincipal();

        relayManager = Object.FindFirstObjectByType<RelayManager>();
        netManager = NetworkManager.Singleton;
        transport = netManager != null ? netManager.GetComponent<UnityTransport>() : null;

        gameFlowManager = GameFlowManager.Instance;

        if (hostIpDisplay != null)
            hostIpDisplay.text = $"Meu IP local {GetLocalIPAddress()}\n";

        // Garante que o texto comece desligado/limpo
        if (textoStatus != null) textoStatus.gameObject.SetActive(false);
    }

    public void MostrarPrincipal() => AtivarSomente(painelPrincipal);
    public void MostrarOpcoes() => AtivarSomente(painelOpcoes);
    public void MostrarCreditos() => AtivarSomente(painelCreditos);
    public void MostrarMultiplayer() => AtivarSomente(painelMultiplayer);
    public void MostrarLan() => AtivarSomente(painelLan);
    public void MostrarModo() => AtivarSomente(painelModo);

    private void AtivarSomente(GameObject alvo)
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
        painelMultiplayer.SetActive(false);
        painelLan.SetActive(false);
        if (painelModo != null) painelModo.SetActive(false);

        if (textoStatus != null) textoStatus.gameObject.SetActive(false);

        alvo.SetActive(true);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // RELAY
    public async void OnHostButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null) return;

        if (textoStatus != null)
        {
            textoStatus.gameObject.SetActive(true);
            textoStatus.text = "Criando sala...";
        }

        Debug.Log("[MainMenu] Criando Relay...");

        string joinCode = await relayManager.CreateRelay(2);

        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"[MainMenu] Relay criado com código: {joinCode}");

            if (relayCodeText != null)
                relayCodeText.text = $"Código da Sala: {joinCode}";

            if (textoStatus != null)
                textoStatus.text = "Você está hosteando! Aguardando...";

            StartCoroutine(WaitForPlayersAndLoadScene());
        }
        else
        {
            Debug.LogError("[MainMenu] Falha ao criar Relay!");
        }
    }

    public async void OnJoinButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null) return;

        string joinCode = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(joinCode)) return;

        bool success = await relayManager.JoinRelay(joinCode);

        if (success)
        {
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
    }

    private IEnumerator WaitForPlayersAndLoadScene()
    {
        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
            yield return null;

        gameFlowManager.TransitionToScene(firstGameSceneName);
    }
    // LAN HOST
    public void OnHostLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null) return;

        Debug.Log($"[MainMenu][LAN] Iniciando HOST...");

        transport.SetConnectionData("0.0.0.0", (ushort)lanPort);

        bool success = netManager.StartHost();

        if (success)
        {
            if (textoStatus != null)
            {
                textoStatus.gameObject.SetActive(true);
                textoStatus.text = "Você está Hosteando!"; // Texto que aparece quando inicia o host
            }
        }

        if (!success) return;

        netManager.OnClientConnectedCallback += OnClientConnectedToHost;
    }

    private void OnClientConnectedToHost(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost || _lanSceneLoaded) return;

        int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;

        if (connectedCount >= 2)
        {
            _lanSceneLoaded = true;
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
    }

    // --------------------------
    // LAN CLIENT
    // --------------------------
    public void OnJoinLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null) return;

        if (_isConnecting) return;

        string ip = ipInputLan.text.Trim();
        if (string.IsNullOrEmpty(ip)) return;

        if (NetworkManager.Singleton.IsListening)
        {
            _isConnecting = true;
            NetworkManager.Singleton.Shutdown();
            StartCoroutine(StartClientWhenShutdown(ip));
        }
        else
        {
            StartCoroutine(StartClientRoutine(ip));
        }
    }

    private IEnumerator StartClientRoutine(string ip)
    {
        transport.SetConnectionData(ip, (ushort)lanPort);

        yield return null;

        bool success = netManager.StartClient();
        _isConnecting = false;
    }

    private IEnumerator StartClientWhenShutdown(string ip)
    {
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
        StartCoroutine(StartClientRoutine(ip));
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "N/A";
    }
}