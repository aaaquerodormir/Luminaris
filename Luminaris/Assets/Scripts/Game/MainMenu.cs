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

    private void Start()
    {
        MostrarPrincipal();

        if (botaoContinuar != null)
            botaoContinuar.SetActive(SaveSystem.HasSave());

        relayManager = FindObjectOfType<RelayManager>();
        netManager = NetworkManager.Singleton;
        transport = netManager != null ? netManager.GetComponent<UnityTransport>() : null;

        if (hostIpDisplay != null)
            hostIpDisplay.text = $"Meu IP local: {GetLocalIPAddress()}\nPorta: {lanPort}";
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

    //Relay

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

            // Espera até o segundo jogador se conectar antes de iniciar
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
        // Espera até que pelo menos 2 jogadores estejam conectados (Host + Cliente)
        while (NetworkManager.Singleton.ConnectedClients.Count < 2)
        {
            yield return null;
        }

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

    // --- LAN: Host ---
    public void OnHostLanButton()
    {
        if (netManager == null || transport == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager ou UnityTransport ausente!");
            return;
        }

        // Mostra informações de rede
        Debug.Log($"[MainMenu][LAN] Iniciando HOST...");
        Debug.Log($"[MainMenu][LAN] Endereço de escuta: 0.0.0.0:{lanPort}");

        // Comentado: propriedade inválida que causava compilação
        // Debug.Log($"[MainMenu][LAN] Protocolo: {transport.ProtocolType}");

        // Configura transporte para escutar em todas interfaces locais
        transport.SetConnectionData("0.0.0.0", (ushort)lanPort);

        bool success = netManager.StartHost();
        Debug.Log(success
            ? $"[MainMenu][LAN] ✅ Host iniciado com sucesso! Escutando em 0.0.0.0:{lanPort}"
            : "[MainMenu][LAN] ❌ Falha ao iniciar Host.");

        // Mostra IPs disponíveis
        Debug.Log($"[MainMenu][LAN] IPs locais disponíveis:");
        foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                Debug.Log($"   - {ip}");
    }

    // --- LAN: Join ---
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
        Debug.Log($"[MainMenu][LAN] Transport ativo: {(transport != null)} | Network ativo: {NetworkManager.Singleton.IsListening}");

        // Se já existe sessão, precisa aguardar o Shutdown terminar antes de iniciar cliente
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[MainMenu][LAN] ⚠️ Já existe uma sessão ativa de rede. Parando antes de conectar...");
            _isConnecting = true;
            NetworkManager.Singleton.Shutdown();
            StartCoroutine(StartClientWhenShutdown(ip));
        }
        else
        {
            // Sem sessão ativa: iniciar cliente imediatamente
            StartCoroutine(StartClientRoutine(ip));
        }
    }

    // Aguarda o NetworkManager parar de escutar e então inicia o cliente
    private IEnumerator StartClientWhenShutdown(string ip)
    {
        // aguarda até que NetworkManager não esteja mais "listening" (Shutdown aplicado)
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsListening);

        // espera um frame adicional para estabilidade
        yield return null;

        // inicia a rotina normal de start client
        yield return StartCoroutine(StartClientRoutine(ip));
    }

    // Rotina que configura o transporte e inicia o cliente, com timeout/checagens
    private IEnumerator StartClientRoutine(string ip)
    {
        if (transport == null || netManager == null)
        {
            Debug.LogError("[MainMenu][LAN] ❌ NetworkManager ou UnityTransport ausente (durante StartClientRoutine).");
            _isConnecting = false;
            yield break;
        }

        // Configura transporte para se conectar ao IP informado
        transport.SetConnectionData(ip, (ushort)lanPort);
        Debug.Log($"[MainMenu][LAN] transport.SetConnectionData({ip},{lanPort})");

        bool success = netManager.StartClient();

        Debug.Log(success
            ? $"[MainMenu][LAN] ✅ Cliente iniciado. Tentando conectar a {ip}:{lanPort}"
            : "[MainMenu][LAN] ❌ Falha ao iniciar cliente.");

        // inicia monitoração de status
        StartCoroutine(DebugConnectionStatus());

        // limpa flag de conexão após um pequeno delay (ou você pode limpar em DebugConnectionStatus quando conectado/timeout)
        yield return new WaitForSeconds(1f);
        _isConnecting = false;
    }

    // Coroutine que monitora o status da conexão
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
