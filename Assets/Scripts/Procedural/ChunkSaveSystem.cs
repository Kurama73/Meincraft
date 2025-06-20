using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class ChunkSaveSystem
{
    public static string CurrentWorld = "DefaultWorld";
    public static string SavePath => Path.Combine(Application.persistentDataPath, "Saves", CurrentWorld, "Chunks");

    public static bool ChunkExists(Vector2Int coord)
    {
        string path = Path.Combine(SavePath, GetFileName(coord));
        return File.Exists(path);
    }

    public static void SaveChunkData(ChunkData data)
    {
        try
        {
            Directory.CreateDirectory(SavePath);
            string path = Path.Combine(SavePath, GetFileName(data.coord));
            string tempPath = path + ".tmp";

            string jsonData = JsonUtility.ToJson(new SerializableChunkData(data), true);
            File.WriteAllText(tempPath, jsonData, Encoding.UTF8);

            if (File.Exists(path)) File.Delete(path);
            File.Move(tempPath, path);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save chunk data: {e.Message}");
        }
    }

    public static ChunkData LoadChunkData(Vector2Int coord)
    {
        string path = Path.Combine(SavePath, GetFileName(coord));
        string backup = path + ".bak";

        if (!File.Exists(path))
        {
            if (File.Exists(backup))
                path = backup;
            else
            {
                Debug.LogWarning($"Chunk not found: {path}");
                return null;
            }
        }

        try
        {
            if (!File.Exists(backup)) File.Copy(path, backup);
            string jsonData = File.ReadAllText(path, Encoding.UTF8);
            SerializableChunkData sData = JsonUtility.FromJson<SerializableChunkData>(jsonData);
            if (sData == null || sData.blocks == null) throw new Exception("Corrupted data");
            return sData.ToChunkData();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load chunk: {path} – {e.Message}");
            if (File.Exists(backup))
            {
                try
                {
                    string jsonData = File.ReadAllText(backup, Encoding.UTF8);
                    SerializableChunkData sData = JsonUtility.FromJson<SerializableChunkData>(jsonData);
                    return sData.ToChunkData();
                }
                catch { Debug.LogError("Backup also corrupted"); }
            }
            return null;
        }
    }

    private static string GetFileName(Vector2Int coord)
    {
        return $"chunk_{coord.x}_{coord.y}.json";
    }
}
