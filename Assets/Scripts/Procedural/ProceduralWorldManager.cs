using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProceduralWorldManager : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSeed = 12345;
    public int seaLevel = 32;
    public int maxWorldHeight = 64;

    [Header("Terrain Generation")]
    public float continentScale = 0.003f;
    public float elevationScale = 0.008f;
    public float ridgeScale = 0.02f;
    public float detailScale = 0.1f;

    [Header("Biome Generation")]
    public float temperatureScale = 0.005f;
    public float humidityScale = 0.007f;
    public float biomeTransitionRadius = 32f;

    [Header("Surface Layers")]
    public int grassLayerThickness = 1;
    public int minDirtThickness = 3;
    public int maxDirtThickness = 8;
    public int bedrockDepth = 5;

    [Header("References")]
    public ChunkManager chunkManager;
    public Transform player;
    public GameObject loadingScreen; // Écran de chargement
    public SaveManager saveManager;

    void Start()
    {
        // Charger la seed et la sauvegarde du monde
        string world = PlayerPrefs.GetString("SelectedWorld", null);

        if (string.IsNullOrEmpty(world))
        {
            Debug.LogWarning("Aucun monde sélectionné. Retour au menu principal.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
            return;
        }

        if (saveManager != null)
        {
            saveManager.currentWorld = world;
            ChunkSaveSystem.CurrentWorld = saveManager.currentWorld;
            SaveSystem.CreateWorldFolder(world);

            int loadedSeed = saveManager.LoadWorldSeed();
            if (loadedSeed != -1)
            {
                worldSeed = loadedSeed;
                Debug.Log("Seed chargée : " + worldSeed);
            }
            else
            {
                Debug.LogWarning("Aucune seed chargée, utilisation de la seed par défaut.");
            }
        }

        // Initialiser le générateur aléatoire avec la seed pour Random.Range et autres
        Random.InitState(worldSeed);

        StartCoroutine(OptimizationLoop());
        StartCoroutine(GenerateInitialChunksAndSpawnPlayer());
    }

    IEnumerator GenerateInitialChunksAndSpawnPlayer()
    {
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Position de spawn : sauvegardée ou générée
        Vector3 spawnPosition = Vector3.zero;

        if (saveManager != null)
        {
            Vector3 savedPosition = saveManager.LoadPlayerPosition();
            if (savedPosition != Vector3.zero)
                spawnPosition = savedPosition;
            else
            {
                spawnPosition = GetSpawnPosition();
                saveManager.SavePlayerPosition(spawnPosition);
            }
        }
        else
        {
            spawnPosition = GetSpawnPosition();
        }

        Vector2Int spawnChunkCoord = chunkManager.GetChunkCoordFromWorldPos(spawnPosition);

        // Générer chunk de spawn + voisins 3x3 en priorité
        List<Vector2Int> priorityChunks = new List<Vector2Int>();
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                priorityChunks.Add(new Vector2Int(spawnChunkCoord.x + x, spawnChunkCoord.y + z));

        foreach (var coord in priorityChunks)
        {
            if (!chunkManager.loadedChunks.ContainsKey(coord))
                yield return StartCoroutine(chunkManager.CreateChunkStaged(coord));
        }

        // Attendre que le chunk spawn soit prêt (MeshCollider actif)
        while (!chunkManager.loadedChunks.TryGetValue(spawnChunkCoord, out var chunk) ||
               chunk.GetComponent<MeshCollider>() == null ||
               !chunk.GetComponent<MeshCollider>().enabled)
        {
            yield return null;
        }

        // Calculer la hauteur réelle à la position de spawn selon le biome
        float surfaceY = GetHeightAtPosition(spawnPosition.x, spawnPosition.z, DetermineBiome(spawnPosition.x, spawnPosition.z));
        Vector3 finalSpawnPos = new Vector3(spawnPosition.x, surfaceY + 1.5f, spawnPosition.z);

        // Désactiver physique pendant placement
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        bool wasKinematic = false;

        if (playerRb != null)
        {
            wasKinematic = playerRb.isKinematic;
            playerRb.isKinematic = true;
        }

        player.position = finalSpawnPos;

        // Attendre que le joueur soit bien au sol en ajustant sa position avec un raycast vers le bas
        bool grounded = false;
        int tries = 0;
        while (!grounded && tries < 50)
        {
            Ray ray = new Ray(player.position, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 10f))
            {
                player.position = hit.point + Vector3.up * 1.5f;
                grounded = true;
            }
            else
            {
                player.position += Vector3.down * 0.5f;
                yield return new WaitForSeconds(0.05f);
                tries++;
            }
        }

        yield return null;

        if (playerRb != null)
            playerRb.isKinematic = wasKinematic;

        if (saveManager != null)
            saveManager.LoadPlayer();

        if (loadingScreen != null)
            loadingScreen.SetActive(false);

        // Lancer la génération des autres chunks
        chunkManager.GenerateInitialChunks();
    }


    IEnumerator OptimizationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (Time.frameCount % 300 == 0)
                System.GC.Collect();
        }
    }

    public float GetHeightAtPosition(float x, float z, BiomeType biomeType)
    {
        float continentNoise = Mathf.PerlinNoise(x * continentScale + worldSeed, z * continentScale + worldSeed);
        float elevationNoise = Mathf.PerlinNoise(x * elevationScale + worldSeed, z * elevationScale + worldSeed) * 0.5f;
        float ridgeNoise = Mathf.PerlinNoise(x * ridgeScale + worldSeed, z * ridgeScale + worldSeed) * 0.25f;
        float detailNoise = Mathf.PerlinNoise(x * detailScale + worldSeed, z * detailScale + worldSeed) * 0.1f;

        float combinedNoise = (continentNoise + elevationNoise + ridgeNoise + detailNoise) / 1.85f;

        var biomeData = GetBiomeData(biomeType);
        return seaLevel + biomeData.baseHeight + (combinedNoise * biomeData.heightVariation);
    }

    public BiomeType DetermineBiome(float x, float z)
    {
        float temperature = Mathf.PerlinNoise(x * temperatureScale + worldSeed, z * temperatureScale + worldSeed);
        float humidity = Mathf.PerlinNoise(x * humidityScale + worldSeed, z * humidityScale + worldSeed);
        float continentNoise = Mathf.PerlinNoise(x * continentScale + worldSeed, z * continentScale + worldSeed);

        temperature = Mathf.Clamp01(temperature);
        humidity = Mathf.Clamp01(humidity);
        continentNoise = Mathf.Clamp01(continentNoise);

        if (continentNoise < 0.25f)
            return BiomeType.Ocean;

        if (temperature > 0.8f && humidity < 0.2f)
            return BiomeType.Desert;
        else if (temperature > 0.6f && humidity > 0.7f)
            return BiomeType.Forest;
        else if (continentNoise > 0.7f && temperature < 0.4f)
            return BiomeType.Mountain;
        else
            return BiomeType.Plains;
    }

    private (float baseHeight, float heightVariation) GetBiomeData(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Ocean: return (-8f, 3f);
            case BiomeType.Desert: return (2f, 8f);
            case BiomeType.Mountain: return (15f, 25f);
            case BiomeType.Forest: return (1f, 12f);
            default: return (0f, 6f); // Plains
        }
    }

    public float GetBlendedHeight(float x, float z)
    {
        var samples = new Dictionary<BiomeType, float>();
        float totalWeight = 0f;
        int sampleRadius = 3;

        for (int dx = -sampleRadius; dx <= sampleRadius; dx++)
        {
            for (int dz = -sampleRadius; dz <= sampleRadius; dz++)
            {
                float sampleX = x + dx * 8f;
                float sampleZ = z + dz * 8f;

                BiomeType biome = DetermineBiome(sampleX, sampleZ);
                float distance = Mathf.Sqrt(dx * dx + dz * dz);
                float weight = 1f / (1f + distance * distance);

                if (!samples.ContainsKey(biome))
                    samples[biome] = 0f;

                samples[biome] += weight;
                totalWeight += weight;
            }
        }

        float blendedHeight = 0f;
        foreach (var sample in samples)
        {
            float biomeWeight = sample.Value / totalWeight;
            float biomeHeight = GetHeightForBiome(x, z, sample.Key);
            blendedHeight += biomeHeight * biomeWeight;
        }

        return blendedHeight;
    }

    private float GetHeightForBiome(float x, float z, BiomeType biomeType)
    {
        float continentNoise = Mathf.PerlinNoise(x * continentScale + worldSeed, z * continentScale + worldSeed);
        float elevationNoise = Mathf.PerlinNoise(x * elevationScale + worldSeed, z * elevationScale + worldSeed) * 0.5f;
        float ridgeNoise = Mathf.PerlinNoise(x * ridgeScale + worldSeed, z * ridgeScale + worldSeed) * 0.25f;
        float detailNoise = Mathf.PerlinNoise(x * detailScale + worldSeed, z * detailScale + worldSeed) * 0.1f;

        float combinedNoise = (continentNoise + elevationNoise + ridgeNoise + detailNoise) / 1.85f;
        var biomeData = GetBiomeData(biomeType);
        return seaLevel + biomeData.baseHeight + (combinedNoise * biomeData.heightVariation);
    }

    public BlockType GetBlockAtPosition(int x, int y, int z)
    {
        float terrainHeight = GetBlendedHeight(x, z);
        int surfaceLevel = Mathf.FloorToInt(terrainHeight);

        if (y <= bedrockDepth)
            return BlockType.bedrock;

        if (y > surfaceLevel)
            return BlockType.air;

        if (y == surfaceLevel)
        {
            BiomeType biome = DetermineBiome(x, z);
            return GetSurfaceBlock(biome);
        }

        int dirtThickness = Random.Range(minDirtThickness, maxDirtThickness + 1);
        if (y > surfaceLevel - dirtThickness)
            return BlockType.dirt;

        return BlockType.stone;
    }

    private BlockType GetSurfaceBlock(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert:
            case BiomeType.Ocean:
                return BlockType.sand;
            default:
                return BlockType.grass;
        }
    }

    public Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;

        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = Random.Range(-50f, 50f);
            float z = Random.Range(-50f, 50f);

            BiomeType biome = DetermineBiome(x, z);
            if (biome != BiomeType.Ocean)
            {
                float height = GetBlendedHeight(x, z);
                int surfaceLevel = Mathf.FloorToInt(height);
                spawnPos = new Vector3(x, surfaceLevel + 2f, z);
                break;
            }
        }

        return spawnPos;
    }

    void OnApplicationQuit()
    {
        if (saveManager != null)
        {
            saveManager.SavePlayer();
            saveManager.SavePlayerPosition();
        }
    }
}
