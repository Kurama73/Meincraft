using System;
using System.IO;
using UnityEngine;

public static class ChunkSaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Chunks");

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

            SerializableChunkData serializableData = new SerializableChunkData(data);
            string jsonData = JsonUtility.ToJson(serializableData);

            File.WriteAllText(path, jsonData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save chunk data: {e.Message}");
        }
    }

    public static ChunkData LoadChunkData(Vector2Int coord)
    {
        string path = Path.Combine(SavePath, GetFileName(coord));
        if (!File.Exists(path))
        {
            Debug.LogWarning($"Chunk file not found: {path}");
            return null;
        }

        try
        {
            string jsonData = File.ReadAllText(path);
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError($"Failed to load chunk data: File is empty.");
                return null;
            }

            SerializableChunkData serializableData = JsonUtility.FromJson<SerializableChunkData>(jsonData);
            if (serializableData == null)
            {
                Debug.LogError($"Failed to load chunk data: Deserialization failed.");
                return null;
            }

            return serializableData.ToChunkData();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load chunk data: {e.Message}");
            return null;
        }
    }

    private static string GetFileName(Vector2Int coord)
    {
        return $"chunk_{coord.x}_{coord.y}.json";
    }
}
