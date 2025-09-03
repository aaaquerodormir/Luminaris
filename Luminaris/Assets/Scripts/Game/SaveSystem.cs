using UnityEngine;
using System.IO;

[System.Serializable]
public class SaveData
{
    // Progresso do jogo
    public int checkpointIndex;
    public GameManager.Difficulty difficulty;

    // Configurações de opções
    public int resolucaoIndex;
    public bool fullscreen;
    public float volume;
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
            return new SaveData(); // Retorna um novo para evitar null
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
