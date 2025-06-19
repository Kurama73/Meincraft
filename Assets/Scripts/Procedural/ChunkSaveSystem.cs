using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public static class ChunkSaveSystem
{
    public static string CurrentWorld = "DefaultWorld";
    private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves", CurrentWorld, "Chunks");

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

            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                SerializableChunkData serializableData = new SerializableChunkData(data);
                formatter.Serialize(stream, serializableData);
            }
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
            IFormatter formatter = new BinaryFormatter();
            using (Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                SerializableChunkData serializableData = (SerializableChunkData)formatter.Deserialize(stream);
                return serializableData.ToChunkData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load chunk data at {path}: {e}");
            return null;
        }
    }

    private static string GetFileName(Vector2Int coord)
    {
        return $"chunk_{coord.x}_{coord.y}.dat";
    }
}