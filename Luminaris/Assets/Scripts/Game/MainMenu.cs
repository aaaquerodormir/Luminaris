using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject painelPrincipal;
    [SerializeField] private GameObject painelOpcoes;
    [SerializeField] private GameObject painelCreditos;
    [SerializeField] private GameObject botaoContinuar;
    [SerializeField] private string gameSceneName = "Game";

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

    private void AtivarSomente(GameObject alvo)
    {
        painelPrincipal.SetActive(false);
        painelOpcoes.SetActive(false);
        painelCreditos.SetActive(false);
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
}
    