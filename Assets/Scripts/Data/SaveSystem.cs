using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static string GetSaveDirectory() =>
        Path.Combine(Application.persistentDataPath, "Saves");

    public static string GetWorldPath(string worldName) =>
        Path.Combine(GetSaveDirectory(), worldName);

    public static void CreateWorldFolder(string worldName)
    {
        string path = GetWorldPath(worldName);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    public static void DeleteWorld(string worldName)
    {
        string path = GetWorldPath(worldName);
        if (Directory.Exists(path))
            Directory.Delete(path, true);
    }

    public static string[] GetWorlds()
    {
        string path = GetSaveDirectory();
        if (!Directory.Exists(path)) return new string[0];
        return Directory.GetDirectories(path);
    }

    public static void SaveWorldSeed(string worldName, int seed)
    {
        string path = Path.Combine(GetWorldPath(worldName), "meta.json");
        WorldMetaData data = new WorldMetaData(seed);
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    public static int LoadWorldSeed(string worldName)
    {
        string path = Path.Combine(GetWorldPath(worldName), "meta.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            WorldMetaData data = JsonUtility.FromJson<WorldMetaData>(json);
            return data.seed;
        }

        Debug.LogWarning("Seed non trouvée pour le monde : " + worldName);
        return 12345; // fallback
    }

}
