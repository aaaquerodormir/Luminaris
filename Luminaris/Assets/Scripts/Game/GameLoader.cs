using UnityEngine;

public class GameLoader : MonoBehaviour
{
    [SerializeField] private Checkpoint[] checkpointsPlayer1;
    [SerializeField] private Checkpoint[] checkpointsPlayer2;
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;
    [SerializeField] private LavaRise lava;

    private void Start()
    {
        SaveData data = SaveSystem.LoadGame();

        if (data != null && data.checkpointIndex >= 0)
        {
            int checkpointIndex = Mathf.Clamp(
                data.checkpointIndex,
                0,
                Mathf.Min(checkpointsPlayer1.Length, checkpointsPlayer2.Length) - 1
            );

            player1.position = checkpointsPlayer1[checkpointIndex].RespawnPosition;
            player2.position = checkpointsPlayer2[checkpointIndex].RespawnPosition;

            float safeZone = Mathf.Min(
                checkpointsPlayer1[checkpointIndex].LavaSafeHeight,
                checkpointsPlayer2[checkpointIndex].LavaSafeHeight
            );

            lava.SetSafeZone(safeZone);
            lava.transform.position = new Vector3(
                lava.transform.position.x,
                safeZone,
                lava.transform.position.z
            );

            Debug.Log("Jogo carregado no checkpoint " + checkpointIndex);
        }
        else
        {
            // Novo jogo ou sem checkpoint alcançado ainda
            player1.position = checkpointsPlayer1[0].RespawnPosition;
            player2.position = checkpointsPlayer2[0].RespawnPosition;

            float safeZone = Mathf.Min(
                checkpointsPlayer1[0].LavaSafeHeight,
                checkpointsPlayer2[0].LavaSafeHeight
            );

            lava.SetSafeZone(safeZone);
            lava.transform.position = new Vector3(
                lava.transform.position.x,
                safeZone,
                lava.transform.position.z
            );

            Debug.Log("Novo jogo iniciado no checkpoint 0");
        }
    }
}
