using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [SerializeField] private Checkpoint[] checkpoints;
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;
    [SerializeField] private LavaRise lava;

    private void Start()
    {
        SaveData data = SaveSystem.LoadGame();

        // Define dificuldade
        GameManager.Instance.SetDifficulty((int)data.difficulty);

        if (data.checkpointIndex >= 0 && data.checkpointIndex < checkpoints.Length)
        {
            var checkpoint = checkpoints[data.checkpointIndex];

            player1.position = checkpoint.RespawnPosition;
            player2.position = checkpoint.RespawnPosition;

            lava.SetSafeZone(checkpoint.LavaSafeHeight);
            Debug.Log("Jogo carregado no checkpoint " + checkpoint.Index);
        }
    }
}
