using UnityEngine;

public class TreeGenerator
{
    public ChunkManager chunkManager;

    public void GenerateTree(Vector3 position)
    {
        BatchUpdate(() => {
            // Tronc
            for (int y = 0; y < 5; y++)
            {
                SetBlock(position.x, position.y + y, position.z, BlockType.spruce_log);
            }

            // Feuillage
            GenerateFoliage(position + Vector3.up * 5);
        });
    }

    void GenerateFoliage(Vector3 center)
    {
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                if (Mathf.Abs(x) == 2 && Mathf.Abs(z) == 2) continue;
                SetBlock(center.x + x, center.y, center.z + z, BlockType.spruce_leaves);

                // Couche supérieure
                if (x == 0 || z == 0)
                    SetBlock(center.x + x, center.y + 1, center.z + z, BlockType.spruce_leaves);
            }
        }
    }

    void BatchUpdate(System.Action action)
    {
        action.Invoke();
    }

    void SetBlock(float x, float y, float z, BlockType type)
    {
        Vector3 worldPos = new Vector3(x, y, z);
        Vector2Int chunkCoord = chunkManager.GetChunkCoordFromWorldPos(worldPos);

        if (chunkManager.loadedChunks.TryGetValue(chunkCoord, out Chunk chunk))
        {
            chunk.SetBlockWithoutRefresh(worldPos, type);
            chunk.RefreshChunk(); // Appeler explicitement le refresh
        }
    }
}