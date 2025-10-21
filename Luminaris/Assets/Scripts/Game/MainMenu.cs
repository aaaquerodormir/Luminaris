using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject painelMultiplayer;
    //[SerializeField] private GameObject painelCodigo; // onde aparece o código do Relay
    //[SerializeField] private Text codigoRelayText;

    [Header("Botões")]
    [SerializeField] private GameObject botaoContinuar;

    [Header("Configurações")]
    [SerializeField] private string gameSceneName = "SampleScene";
    //[SerializeField] private InputField joinCodeInput;

    [Header("Relay UI")]
    [SerializeField] private TMP_Text relayCodeText;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("LAN UI")]
    [SerializeField] private TMP_InputField ipAddressInput; // NOVO CAMPO

    private RelayManager relayManager;

    private void Start()
    {
        MostrarPrincipal();

        if (botaoContinuar != null)
            botaoContinuar.SetActive(SaveSystem.HasSave());

        relayManager = FindObjectOfType<RelayManager>();
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

    private void AtivarSomente(GameObject alvo)
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
        painelMultiplayer.SetActive(false);
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

    // ===============================
    // ======= MULTIPLAYER ===========
    // ===============================

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

    // --- NOVOS MÉTODOS LAN ---

    public void OnLanHostButtonPressed()
    {
        if (relayManager == null) return;

        relayManager.StartLanHost();
        if (relayCodeText != null)
        {
            relayCodeText.text = "Sala LAN criada!\nPeça para os outros se conectarem ao seu IP.";
        }
        StartCoroutine(WaitForPlayersAndLoadScene());
    }

    public void OnLanJoinButtonPressed()
    {
        if (relayManager == null) return;
        string ipAddress = ipAddressInput.text.Trim();
        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogWarning("[MainMenu] O endereço de IP não pode estar vazio!");
            return;
        }
        relayManager.JoinLanClient(ipAddress);
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
}
