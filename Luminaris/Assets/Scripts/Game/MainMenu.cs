using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
public class MainMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private GameObject painelMultiplayer;
    //[SerializeField] private string gameSceneName = "Game";

    [Header("Config")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private InputField ipInput;
    [SerializeField] private ushort port = 7777;

    private void Start()
    {
        MostrarPrincipal();

        if (botaoContinuar != null)
            botaoContinuar.SetActive(SaveSystem.HasSave());
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

    // Botões Multiplayer
    public void HostGame()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        var address = ipInput != null && !string.IsNullOrEmpty(ipInput.text) ? ipInput.text : "127.0.0.1";
        var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ut.SetConnectionData(address, port);
        NetworkManager.Singleton.StartClient();
    }

    public void VoltarDoMultiplayer()
    {
        MostrarPrincipal();
    }
}
    