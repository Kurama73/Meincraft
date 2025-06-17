using UnityEngine;
using System.Collections;

public class ProceduralWorldManager : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSeed = 12345;
    public float noiseScale = 0.1f;
    public int maxHeight = 10;

    [Header("Performance")]
    public int chunksPerFrame = 1;
    public float updateInterval = 0.1f;

    [Header("References")]
    public ChunkManager chunkManager;
    public Transform player;

    void Start()
    {
        Random.InitState(worldSeed);
        StartCoroutine(OptimizationLoop());
        StartCoroutine(GenerateInitialChunksAndSpawnPlayer());
    }

    IEnumerator GenerateInitialChunksAndSpawnPlayer()
    {
        yield return new WaitForSeconds(1f);
        player.position = GetSpawnPosition();
    }

    IEnumerator OptimizationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            if (Time.frameCount % 300 == 0)
            {
                System.GC.Collect();
            }
        }
    }

    public Biome DetermineBiome(float x, float z)
    {
        float biomeNoise = Mathf.PerlinNoise(x * 0.01f, z * 0.01f); // Utilisez une échelle plus petite pour des biomes plus grands

        if (biomeNoise < 0.2f)
        {
            return new Biome(BiomeType.Ocean, -5, 2, Color.blue);
        }
        else if (biomeNoise < 0.3f)
        {
            return new Biome(BiomeType.Desert, 5, 3, Color.yellow);
        }
        else if (biomeNoise < 0.5f)
        {
            return new Biome(BiomeType.Plains, 0, 5, Color.green);
        }
        else if (biomeNoise < 0.7f)
        {
            return new Biome(BiomeType.Forest, 0, 10, Color.green);
        }
        else
        {
            return new Biome(BiomeType.Mountain, 10, 20, Color.gray);
        }
    }



    public float GetHeightAtPosition(float x, float z, Biome biome)
    {
        float noiseValue1 = Mathf.PerlinNoise(x * 0.05f, z * 0.05f);
        float noiseValue2 = Mathf.PerlinNoise(x * 0.1f, z * 0.1f) * 0.5f;
        float noiseValue3 = Mathf.PerlinNoise(x * 0.2f, z * 0.2f) * 0.25f;

        float noiseValue = (noiseValue1 + noiseValue2 + noiseValue3) / 2.75f; // Normaliser la somme des bruits
        return biome.baseHeight + noiseValue * biome.heightVariation;
    }



    public Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;
        spawnPos.y = GetHeightAtPosition(0, 0, new Biome(BiomeType.Plains, 0, 10, Color.green)) + 3f;

        if (spawnPos.y <= 0)
        {
            spawnPos.y = 3f;
        }

        return spawnPos;
    }
}
