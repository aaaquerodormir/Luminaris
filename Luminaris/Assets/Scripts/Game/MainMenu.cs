using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using System.Net;
using System.Net.Sockets;
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

    [Header("Status UI")]
    [SerializeField] private TMP_Text textoStatus;

    [Header("Configurações")]
    [SerializeField] private string firstGameSceneName = "Fase1";

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
            hostIpDisplay.text = $"Meu IP LAN: {GetBestLanIP(lanPort)}";

        if (textoStatus != null) textoStatus.text = "";
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

        if (textoStatus != null) textoStatus.text = "";

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

    public async void OnHostButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null)
        {
            return;
        }
        if (textoStatus != null) textoStatus.text = "Criando sala...";

        string joinCode = await relayManager.CreateRelay(2);

        if (!string.IsNullOrEmpty(joinCode))
        {
            if (relayCodeText != null)
                relayCodeText.text = $"Código da Sala: {joinCode}\nAguardando jogador...";

            if (textoStatus != null) textoStatus.text = "Você está hosteando!";

            StartCoroutine(WaitForPlayersAndLoadScene());
        }
        else
        {
            if (textoStatus != null) textoStatus.text = "Erro ao criar.";
        }
    }

    public async void OnJoinButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null)
        {
            return;
        }

        string joinCode = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            return;
        }

        if (textoStatus != null) textoStatus.text = "Entrando...";

        bool success = await relayManager.JoinRelay(joinCode);

        if (success)
        {
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
        else
        {
            if (textoStatus != null) textoStatus.text = "Falha ao entrar.";
        }
    }

    private IEnumerator WaitForPlayersAndLoadScene()
    {
        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
            yield return null;

        if (relayCodeText != null)
            relayCodeText.text = "Jogador conectado! Iniciando...";

        gameFlowManager.TransitionToScene(firstGameSceneName);
    }

    public void OnHostLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null)
        {
            return;
        }

        transport.SetConnectionData("0.0.0.0", (ushort)lanPort);

        bool success = netManager.StartHost();

        if (!success)
            return;

        if (textoStatus != null)
        {
            textoStatus.gameObject.SetActive(true);
            textoStatus.text = "Você está hosteando!";
        }

        netManager.OnClientConnectedCallback += OnClientConnectedToHost;
    }

    private void OnClientConnectedToHost(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost || _lanSceneLoaded)
            return;

        int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (textoStatus != null) textoStatus.text = $"Jogador conectado ({connectedCount}/2)";

        if (connectedCount >= 2)
        {
            _lanSceneLoaded = true;
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
    }
    public void OnJoinLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null)
        {
            return;
        }

        if (_isConnecting)
        {
            return;
        }

        string ip = ipInputLan.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            return;
        }

        if (textoStatus != null) textoStatus.text = "Conectando...";

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

        if (success)
        {
        }
        else
        {
            if (textoStatus != null) textoStatus.text = "Falha ao conectar.";
        }
        _isConnecting = false;
    }

    private IEnumerator StartClientWhenShutdown(string ip)
    {
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
        StartCoroutine(StartClientRoutine(ip));
    }

    private string GetBestLanIP(int port)
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        var ipv4s = host.AddressList
            .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
            .ToList();

        foreach (var ip in ipv4s)
        {
            try
            {
                using (UdpClient client = new UdpClient(new IPEndPoint(ip, port)))
                {
                    client.EnableBroadcast = true;

                    // Pacote de teste LAN
                    byte[] testMsg = System.Text.Encoding.ASCII.GetBytes("PING");
                    IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, port);

                    // Envia uma vez — se não der exception, IP é válido
                    client.Send(testMsg, testMsg.Length, broadcastEP);

                    return ip.ToString(); // IP válido
                }
            }
            catch
            {
                // Ignora IPs inválidos, VPNs, virtuais, etc.
            }
        }

        // fallback
        return ipv4s.Count > 0 ? ipv4s[0].ToString() : "127.0.0.1";
    }
}