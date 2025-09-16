using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
    // Último grupo sincronizado (GroupId). -1 = nenhum.
    public int checkpointGroup = -1;

    // Progresso da lava (somente salvo se checkpointGroup > 0)
    public int lavaSavedTurns = 0;

    // Configurações
    public int resolucaoIndex = -1;
    public bool fullscreen = true;
    public float volume = 0.5f;
}

public static class SaveSystem
{
    private static string savePath = Application.persistentDataPath + "/save.json";

    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Jogo salvo em: " + savePath);
    }

    public static SaveData LoadGame()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Jogo carregado!");
            return data;
        }
        else
        {
            Debug.LogWarning("Nenhum save encontrado!");
            return null;
        }
    }

    public static bool HasSave()
    {
        return File.Exists(savePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(savePath))
            File.Delete(savePath);
    }
}
