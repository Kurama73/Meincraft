using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct TerrainGenJob : IJob
{
    public int chunkSize;
    public int maxHeight;
    public int worldSeed;
    public float continentScale;
    public float elevationScale;
    public int worldXOffset;
    public int worldZOffset;

    public NativeArray<BlockType> blockData;

    public void Execute()
    {
        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                // Calcul de la hauteur du terrain
                float continent = Mathf.PerlinNoise((x + worldXOffset) * continentScale, (z + worldZOffset) * continentScale);
                float elevation = Mathf.PerlinNoise((x + worldXOffset + 1000) * elevationScale, (z + worldZOffset + 1000) * elevationScale);
                int height = Mathf.Clamp(Mathf.FloorToInt(continent * elevation * maxHeight), 1, maxHeight - 1);

                for (int y = 0; y < maxHeight; y++)
                {
                    int index = x + chunkSize * (z + chunkSize * y);

                    if (y == 0)
                    {
                        blockData[index] = BlockType.bedrock;
                    }
                    else if (y < height - 4)
                    {
                        blockData[index] = BlockType.stone;
                    }
                    else if (y < height - 1)
                    {
                        blockData[index] = BlockType.dirt;
                    }
                    else if (y == height - 1)
                    {
                        blockData[index] = BlockType.grass;
                    }
                    else
                    {
                        blockData[index] = BlockType.air;
                    }
                }
            }
        }
    }
}
