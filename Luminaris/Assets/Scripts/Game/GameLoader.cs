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

        int checkpointIndex = 0; // por padrão começa do primeiro

        if (data != null && data.checkpointIndex >= 0)
        {
            checkpointIndex = Mathf.Clamp(
                data.checkpointIndex,
                0,
                Mathf.Min(checkpointsPlayer1.Length, checkpointsPlayer2.Length) - 1
            );

            Debug.Log("Jogo carregado no checkpoint " + checkpointIndex);
        }
        else
        {
            Debug.Log("Novo jogo iniciado no checkpoint 0");
        }

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

        // Aqui marcamos os checkpoints já alcançados como ativados
        for (int i = 0; i <= checkpointIndex; i++)
        {
            checkpointsPlayer1[i].PreActivate();
            checkpointsPlayer2[i].PreActivate();
        }
    }
}
