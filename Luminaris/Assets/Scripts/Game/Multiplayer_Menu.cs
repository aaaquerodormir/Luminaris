using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MultiplayerMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject painelMultiplayer;
    [SerializeField] private TMP_Text codigoSalaText;   // Mostra o c�digo gerado pelo Host
    [SerializeField] private TMP_InputField inputCodigo; // Jogador digita aqui o c�digo
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private ushort port = 7777;

    private void Start()
    {
        painelMultiplayer.SetActive(false);
    }

    public void AbrirPainelMultiplayer()
    {
        painelMultiplayer.SetActive(true);
        codigoSalaText.text = "";
    }

    public void FecharPainelMultiplayer()
    {
        painelMultiplayer.SetActive(false);
    }

    public void HostGame()
    {
        // Gera "c�digo" = IP do host
        string codigo = GetLocalIPAddress();
        codigoSalaText.text = $"C�digo: {codigo}";

        var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ut.SetConnectionData("0.0.0.0", port); // aceita conex�es de fora
        NetworkManager.Singleton.StartHost();

        // Troca de cena sincronizada
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public void JoinGame()
    {
        string codigo = inputCodigo.text; // jogador digita o c�digo do host (IP)
        if (string.IsNullOrEmpty(codigo)) return;

        var ut = NetworkManager.Singleton.GetComponent<UnityTransport>();
        ut.SetConnectionData(codigo, port);
        NetworkManager.Singleton.StartClient();
    }

    // Pega IP da m�quina do Host
    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch { }
        return "127.0.0.1";
    }
}