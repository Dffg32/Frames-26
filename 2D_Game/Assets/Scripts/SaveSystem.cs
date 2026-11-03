using System.IO;
using UnityEngine;

// What gets written to the save file. The field names are the keys in the JSON:
// { "nivel": 1, "puntos": 1250, "monedas": 12, "vidas": 2 }
[System.Serializable]
public class SaveData
{
    public int nivel = 1;
    public int puntos;
    public int monedas;
    public int vidas = 3;
}

// Reads and writes the save file (partida.json) in Unity's persistent data folder.
// On Windows that's C:\Users\<user>\AppData\LocalLow\<company>\<product>\partida.json
public static class SaveSystem
{
    public static string FilePath => Path.Combine(Application.persistentDataPath, "partida.json");

    public static bool Save(SaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, true));
            Debug.Log($"Partida guardada en {FilePath}");
            return true;
        }
        catch (IOException e)
        {
            Debug.LogWarning($"No se pudo guardar la partida: {e.Message}");
            return false;
        }
    }

    // Returns null when there is no save file or it can't be read.
    public static SaveData Load()
    {
        if (!File.Exists(FilePath)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"No se pudo leer la partida guardada: {e.Message}");
            return null;
        }
    }
}
