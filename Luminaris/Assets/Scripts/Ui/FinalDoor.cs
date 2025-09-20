using UnityEngine;

public class FinalDoor : MonoBehaviour
{
    private bool player1Inside = false;
    private bool player2Inside = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player1"))
        {
            player1Inside = true;
            CheckVictory();
        }
        else if (collision.CompareTag("Player2"))
        {
            player2Inside = true;
            CheckVictory();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player1"))
            player1Inside = false;
        else if (collision.CompareTag("Player2"))
            player2Inside = false;
    }

    private void CheckVictory()
    {
        if (player1Inside && player2Inside)
        {
            GameManager.Instance.ShowVictoryPanel();
        }
    }
}
