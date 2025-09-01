using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject botaoContinuar; // botão "Continuar"

    [Header("Configurações")]
    [SerializeField] private string gameSceneName = "Game"; // Nome da cena do jogo

    private void Start()
    {
        MostrarPainelPrincipal();

        // Só habilita o botão Continuar se existir save
        if (botaoContinuar != null)
            botaoContinuar.SetActive(SaveSystem.HasSave());
    }

    public void NovoJogo()
    {
        // Apaga save e começa do zero
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

    public void AbrirOpcoes()
    {
        painelPrincipal.SetActive(false);
        painelCreditos.SetActive(false);
        painelOpcoes.SetActive(true);
    }

    public void AbrirCreditos()
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(true);
    }

    public void MostrarPainelPrincipal()
    {
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
        painelPrincipal.SetActive(true);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
