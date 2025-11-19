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
    [SerializeField] private GameObject painelModo; // ✅ NOVO painel Modo

    // [Header("Botões")]
    // [SerializeField] private GameObject botaoContinuar;

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

        // if (botaoContinuar != null)
        // botaoContinuar.SetActive(SaveSystem.HasSave());

        relayManager = Object.FindFirstObjectByType<RelayManager>();
        netManager = NetworkManager.Singleton;
        transport = netManager != null ? netManager.GetComponent<UnityTransport>() : null;

        gameFlowManager = GameFlowManager.Instance;

        if (hostIpDisplay != null)
            hostIpDisplay.text = $"Meu IP local {GetLocalIPAddress()}\n";
    }

    //public void NovoJogo()
    //{
    //  SaveSystem.DeleteSave();
    //Time.timeScale = 1f;
    // SceneManager.LoadScene(firstGameSceneName);
    // }

    // public void ContinuarJogo()
    // {
    //  if (SaveSystem.HasSave())
    //  {
    //  Time.timeScale = 1f;
    // SceneManager.LoadScene(firstGameSceneName);
    //  }
    //  }

    public void MostrarPrincipal() => AtivarSomente(painelPrincipal);
    public void MostrarOpcoes() => AtivarSomente(painelOpcoes);
    public void MostrarCreditos() => AtivarSomente(painelCreditos);
    public void MostrarMultiplayer() => AtivarSomente(painelMultiplayer);
    public void MostrarLan() => AtivarSomente(painelLan);
    public void MostrarModo() => AtivarSomente(painelModo); // ✅ NOVO método

    private void AtivarSomente(GameObject alvo)
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
        painelMultiplayer.SetActive(false);
        painelLan.SetActive(false);
        if (painelModo != null) painelModo.SetActive(false); // ✅ NOVO
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

    // --------------------------
    // RELAY (com GameFlowManager)
    // --------------------------
    public async void OnHostButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null)
        {
            Debug.LogError("[MainMenu] RelayManager ou GameFlowManager não encontrado!");
            return;
        }

        Debug.Log("[MainMenu] Criando Relay...");

        string joinCode = await relayManager.CreateRelay(2);

        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"[MainMenu] Relay criado com código: {joinCode}");

            if (relayCodeText != null)
                relayCodeText.text = $"Código da Sala: {joinCode}\nAguardando jogador...";

            StartCoroutine(WaitForPlayersAndLoadScene());
        }
        else
        {
            Debug.LogError("[MainMenu] Falha ao criar Relay!");
        }
    }

    public async void OnJoinButtonPressed()
    {
        if (relayManager == null || gameFlowManager == null)
        {
            Debug.LogError("[MainMenu] RelayManager ou GameFlowManager não encontrado!");
            return;
        }

        string joinCode = joinCodeInput.text.Trim();

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("[MainMenu] Nenhum código foi inserido!");
            return;
        }

        bool success = await relayManager.JoinRelay(joinCode);

        if (success)
        {
            Debug.Log($"[MainMenu] Entrando na sala com código {joinCode}");
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
        else
        {
            Debug.LogError("[MainMenu] Falha ao entrar na sala!");
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

    // --------------------------
    // LAN HOST (com GameFlowManager)
    // --------------------------
    public void OnHostLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager, UnityTransport ou GameFlowManager ausente!");
            return;
        }

        Debug.Log($"[MainMenu][LAN] Iniciando HOST...");
        Debug.Log($"[MainMenu][LAN] Endereço de escuta: 0.0.0.0:{lanPort}");

        transport.SetConnectionData("0.0.0.0", (ushort)lanPort);

        bool success = netManager.StartHost();
        Debug.Log(success
            ? $"[MainMenu][LAN] ✅ Host iniciado com sucesso! Escutando em 0.0.0.0:{lanPort}"
            : "[MainMenu][LAN] ❌ Falha ao iniciar Host.");

        if (!success)
            return;

        netManager.OnClientConnectedCallback += OnClientConnectedToHost;
    }

    private void OnClientConnectedToHost(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost || _lanSceneLoaded)
            return;

        int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[MainMenu][LAN] Cliente conectado! Total: {connectedCount}");

        if (connectedCount >= 2)
        {
            _lanSceneLoaded = true;
            Debug.Log("[MainMenu][LAN] 🟢 Carregando cena LAN sincronizada...");
            gameFlowManager.TransitionToScene(firstGameSceneName);
        }
    }

    // --------------------------
    // LAN CLIENT (com GameFlowManager)
    // --------------------------
    public void OnJoinLanButton()
    {
        if (netManager == null || transport == null || gameFlowManager == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager, UnityTransport ou GameFlowManager ausente!");
            return;
        }

        if (_isConnecting)
        {
            Debug.LogWarning("[MainMenu][LAN] ⚠️ Conexão já em progresso.");
            return;
        }

        string ip = ipInputLan.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("[MainMenu][LAN] ⚠️ Nenhum IP foi inserido!");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[MainMenu][LAN] ⚠️ Já existe uma sessão ativa de rede. Parando antes de conectar...");
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
            Debug.Log($"[MainMenu][LAN] ✅ Conexão iniciada com sucesso! Tentando se conectar a {ip}:{lanPort}");
        }
        else
        {
            Debug.LogError("[MainMenu][LAN] ❌ Falha ao iniciar Cliente.");
        }
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
