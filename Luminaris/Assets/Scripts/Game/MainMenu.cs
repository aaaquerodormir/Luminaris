using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Core;
using TMPro;
using System.Threading.Tasks;
public class MainMenu : MonoBehaviour
{
    [Header("Paineis")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private GameObject painelMultiplayer;
    //[SerializeField] private string gameSceneName = "Game";

    [Header("Multiplayer UI")]
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text joinCodeDisplay;

    [Header("Config")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Start()
    {
        MostrarPrincipal();

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

    // ========= MULTIPLAYER VIA RELAY =========
    public async void HostGame()
    {
        string joinCode = await RelayManager.Instance.CreateRelay();
        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"[MainMenu] Código da sala: {joinCode}");
            if (joinCodeDisplay != null)
                joinCodeDisplay.text = joinCode; // <-- nome atualizado
        }
    }

    public void JoinGame()
    {
        string joinCode = joinCodeInput.text.Trim(); // <-- nome atualizado
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("[MainMenu] Nenhum código inserido.");
            return;
        }

        RelayManager.Instance.JoinRelay(joinCode);
    }
}
