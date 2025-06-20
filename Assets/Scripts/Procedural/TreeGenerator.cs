using UnityEngine;
using System.Collections.Generic;

public class TreeGenerator
{
    public ChunkManager chunkManager;

    public void GenerateDenseForest(Vector2Int coord, Biome biome)
    {
        int spacing = Random.Range(5, 10);
        int treesPerRow = chunkManager.chunkSize / spacing;

        if (!chunkManager.loadedChunks.TryGetValue(coord, out Chunk chunk) || chunk.data == null)
            return;

        for (int i = 0; i < treesPerRow; i++)
        {
            for (int j = 0; j < treesPerRow; j++)
            {
                int localX = i * spacing + Random.Range(0, 2);
                int localZ = j * spacing + Random.Range(0, 2);

                if (localX >= chunkManager.chunkSize || localZ >= chunkManager.chunkSize)
                    continue;

                int groundY = GetSurfaceY(chunk.data, localX, localZ);
                if (groundY <= 0 || groundY >= chunkManager.proceduralWorldManager.maxWorldHeight - 6)
                    continue;

                Vector3 worldPos = new Vector3(
                    coord.x * chunkManager.chunkSize + localX,
                    groundY + 1,
                    coord.y * chunkManager.chunkSize + localZ
                );

                GenerateTree(worldPos);
            }
        }
    }

    int GetSurfaceY(ChunkData data, int x, int z)
    {
        for (int y = chunkManager.proceduralWorldManager.maxWorldHeight - 1; y >= 0; y--)
        {
            BlockType block = data.GetBlock(x, y, z);
            if (block != BlockType.air)
                return y;
        }
        // Pas de bloc solide trouvé, on renvoie une hauteur par défaut au niveau du sol
        return 1;
    }




    public void GenerateTree(Vector3 position)
    {
        List<Vector3> foliage = new List<Vector3>();
        List<Vector3> logs = new List<Vector3>();

        // Tronc
        int height = Random.Range(4, 6);
        for (int y = 0; y < height; y++)
            logs.Add(position + Vector3.up * y);

        // Feuillage
        Vector3 top = position + Vector3.up * height;
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                float dist = Mathf.Abs(x) + Mathf.Abs(z);
                if (dist > 3) continue;

                foliage.Add(top + new Vector3(x, 0, z));

                if (dist <= 1)
                    foliage.Add(top + new Vector3(x, 1, z));
            }
        }

        BatchUpdate(logs, foliage);
    }

    void BatchUpdate(List<Vector3> logs, List<Vector3> foliage)
    {
        HashSet<Vector2Int> modifiedChunks = new HashSet<Vector2Int>();

        foreach (var pos in logs)
        {
            if (SetBlock(pos, BlockType.spruce_log, out Vector2Int coord))
                modifiedChunks.Add(coord);
        }

        foreach (var pos in foliage)
        {
            if (SetBlock(pos, BlockType.spruce_leaves, out Vector2Int coord))
                modifiedChunks.Add(coord);
        }

        // Rafraîchir uniquement les chunks modifiés
        foreach (var coord in modifiedChunks)
        {
            if (chunkManager.loadedChunks.TryGetValue(coord, out Chunk chunk))
                chunk.RefreshChunk();
        }
    }

    bool SetBlock(Vector3 worldPos, BlockType type, out Vector2Int chunkCoord)
    {
        chunkCoord = chunkManager.GetChunkCoordFromWorldPos(worldPos);
        if (chunkManager.loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            chunk.SetBlockWithoutRefresh(worldPos, type);
            return true;
        }
        return false;
    }
}
