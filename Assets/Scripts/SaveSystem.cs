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
}
