using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject opcoesUI;

    private bool jogoPausado = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !GameManager.Instance.gameOverAtivo)
        {
            if (jogoPausado) Continuar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        pauseUI.SetActive(true);
        opcoesUI.SetActive(false);
        Time.timeScale = 0f;
        jogoPausado = true;
    }

    public void Continuar()
    {
        pauseUI.SetActive(false);
        Time.timeScale = 1f;
        jogoPausado = false;
    }

    public void AbrirOpcoes()
    {
        pauseUI.SetActive(false);
        opcoesUI.SetActive(true);
    }

    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
