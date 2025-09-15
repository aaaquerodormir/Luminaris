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

        int group = 0;
        if (data != null && data.checkpointGroup >= 0)
        {
            group = data.checkpointGroup;
            Debug.Log("Jogo carregado no grupo " + group);
        }
        else
        {
            Debug.Log("Novo jogo iniciado no grupo 0");
        }

        Checkpoint cp1 = System.Array.Find(checkpointsPlayer1, c => c.GroupId == group);
        Checkpoint cp2 = System.Array.Find(checkpointsPlayer2, c => c.GroupId == group);

        player1.position = cp1 != null ? cp1.RespawnPosition : checkpointsPlayer1[0].RespawnPosition;
        player2.position = cp2 != null ? cp2.RespawnPosition : checkpointsPlayer2[0].RespawnPosition;

        float safeZone = Mathf.Min(
            cp1 != null ? cp1.LavaSafeHeight : checkpointsPlayer1[0].LavaSafeHeight,
            cp2 != null ? cp2.LavaSafeHeight : checkpointsPlayer2[0].LavaSafeHeight
        );

        lava.SetSafeZone(safeZone);
        lava.transform.position = new Vector3(
            lava.transform.position.x,
            safeZone,
            lava.transform.position.z
        );

        foreach (var c in checkpointsPlayer1)
            if (c.GroupId <= group) c.PreActivate();

        foreach (var c in checkpointsPlayer2)
            if (c.GroupId <= group) c.PreActivate();
    }
}
