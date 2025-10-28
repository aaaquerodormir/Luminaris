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
    //[SerializeField] private GameObject painelCodigo; // onde aparece o código do Relay
    //[SerializeField] private Text codigoRelayText;
    [SerializeField] private GameObject painelLan;

    [Header("Botões")]
    [SerializeField] private GameObject botaoContinuar;

    [Header("Configurações")]
    [SerializeField] private string gameSceneName = "SampleScene";
    //[SerializeField] private InputField joinCodeInput;

    [Header("Relay UI")]
    [SerializeField] private TMP_Text relayCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("LAN UI")]
    [SerializeField] private TMP_InputField ipInputLan;      // Campo de entrada IP
    [SerializeField] private TMP_Text hostIpDisplay;         // Mostra IP do host
    [SerializeField] private int lanPort = 7777;

    private RelayManager relayManager;
    private UnityTransport transport;
    private NetworkManager netManager;

    // Flag para evitar chamadas concorrentes ao iniciar cliente
    private bool _isConnecting = false;
    private bool _lanSceneLoaded = false;

    private void Start()
    {
        MostrarPrincipal();

        if (botaoContinuar != null)
            botaoContinuar.SetActive(SaveSystem.HasSave());

        relayManager = FindObjectOfType<RelayManager>();
        netManager = NetworkManager.Singleton;
        transport = netManager != null ? netManager.GetComponent<UnityTransport>() : null;

        if (hostIpDisplay != null)
            hostIpDisplay.text = $"Meu IP local: {GetLocalIPAddress()}\n";
    }

    public void NovoJogo()
    {
        SaveSystem.DeleteSave();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ContinuarJogo()
    {
        if (SaveSystem.HasSave())
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void MostrarPrincipal() => AtivarSomente(painelPrincipal);
    public void MostrarOpcoes() => AtivarSomente(painelOpcoes);
    public void MostrarCreditos() => AtivarSomente(painelCreditos);
    public void MostrarMultiplayer() => AtivarSomente(painelMultiplayer);
    public void MostrarLan() => AtivarSomente(painelLan);

    private void AtivarSomente(GameObject alvo)
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
        painelMultiplayer.SetActive(false);
        painelLan.SetActive(false);
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
    // RELAY (mantido intacto)
    // --------------------------
    public async void OnHostButtonPressed()
    {
        if (relayManager == null)
        {
            Debug.LogError("[MainMenu] RelayManager não encontrado na cena!");
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
        if (relayManager == null)
        {
            Debug.LogError("[MainMenu] RelayManager não encontrado!");
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
            StartCoroutine(LoadSceneAfterDelay(1f));
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

        yield return new WaitForSeconds(1f);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    // --------------------------
    // LAN HOST
    // --------------------------
    public void OnHostLanButton()
    {
        if (netManager == null || transport == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager ou UnityTransport ausente!");
            return;
        }

        Debug.Log($"[MainMenu][LAN] Iniciando HOST...");
        Debug.Log($"[MainMenu][LAN] Endereço de escuta: 0.0.0.0:{lanPort}");

        transport.SetConnectionData("0.0.0.0", (ushort)lanPort);

        bool success = netManager.StartHost();
        Debug.Log(success
            ? $"[MainMenu][LAN] ✅ Host iniciado com sucesso! Escutando em 0.0.0.0:{lanPort}"
            : "[MainMenu][LAN] ❌ Falha ao iniciar Host.");

        if (!success) return;

        // Evento: quando cliente conectar, o host troca de cena
        netManager.OnClientConnectedCallback += OnClientConnectedToHost;

        // OBS: havia aqui uma inscrição em OnLoadEventCompleted que depende da versão do NGO.
        // Essa inscrição foi removida para evitar incompatibilidades de assinatura do delegate.
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
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }

    // --------------------------
    // LAN CLIENT
    // --------------------------
    public void OnJoinLanButton()
    {
        if (netManager == null || transport == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager ou UnityTransport ausente!");
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

        Debug.Log($"[MainMenu][LAN] Tentando se conectar ao host {ip}:{lanPort}");

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

    private IEnumerator StartClientWhenShutdown(string ip)
    {
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);
        yield return null;
        yield return StartCoroutine(StartClientRoutine(ip));
    }

    private IEnumerator StartClientRoutine(string ip)
    {
        if (transport == null || netManager == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager ou UnityTransport ausente (durante StartClientRoutine).");
            _isConnecting = false;
            yield break;
        }

        transport.SetConnectionData(ip, (ushort)lanPort);
        Debug.Log($"[MainMenu][LAN] transport.SetConnectionData({ip},{lanPort})");

        bool success = netManager.StartClient();

        Debug.Log(success
            ? $"[MainMenu][LAN] ✅ Cliente iniciado. Tentando conectar a {ip}:{lanPort}"
            : "[MainMenu][LAN] ❌ Falha ao iniciar cliente.");

        StartCoroutine(DebugConnectionStatus());
        yield return new WaitForSeconds(1f);
        _isConnecting = false;
    }

    private IEnumerator DebugConnectionStatus()
    {
        float timer = 0f;
        while (timer < 10f)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log($"[MainMenu][LAN] 🟢 Cliente conectado com sucesso ao Host!");
                yield break;
            }

            Debug.Log($"[MainMenu][LAN] Tentando conectar... ({timer:F1}s)");
            timer += 1f;
            yield return new WaitForSeconds(1f);
        }

        Debug.LogError("[MainMenu][LAN] ❌ Timeout de conexão após 10 segundos.");
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            return ip != null ? ip.ToString() : "IP não disponível";
        }
        catch
        {
            return "IP não disponível";
        }
    }
}
