using UnityEngine;

public class VictoryManager : MonoBehaviour
{
    private static VictoryManager instance;
    private FinalDoor[] doors;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        doors = FindObjectsByType<FinalDoor>(FindObjectsSortMode.None);
    }

    public static void CheckVictory()
    {
        if (instance == null) return;

        foreach (var door in instance.doors)
        {
            if (door == null || !door.IsPlayerInside)
                return; // algum jogador ainda n�o chegou
        }

        GameManager.Instance.ShowVictoryPanel();
    }

    // M�todo para bot�o Menu Principal do VictoryMenuWrapper
    public void OnClickMenuPrincipal()
    {
        GameManager.Instance.OpenVictoryConfirmation(() =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        });
    }
}