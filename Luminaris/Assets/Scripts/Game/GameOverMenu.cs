using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private GameObject confirmacaoPanel;

    private System.Action acaoConfirmada;

    public void TentarNovamente()
    {
        gameManager.TentarNovamente();
    }

    public void VoltarMenuPrincipal()
    {
        // Mostra o painel de confirmação antes de sair
        AbrirConfirmacao(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        });
    }

    public void SairDoJogo()
    {
        // Mostra o painel de confirmação antes de sair
        AbrirConfirmacao(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private void AbrirConfirmacao(System.Action acao)
    {
        confirmacaoPanel.SetActive(true);
        acaoConfirmada = acao;
    }

    public void Confirmar()
    {
        confirmacaoPanel.SetActive(false);
        acaoConfirmada?.Invoke();
    }

    public void Cancelar()
    {
        confirmacaoPanel.SetActive(false);
        acaoConfirmada = null;
    }
}
