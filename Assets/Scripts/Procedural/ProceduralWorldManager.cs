using UnityEngine;
using System.Collections;

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

    void Start()
    {
        Random.InitState(worldSeed);
        StartCoroutine(OptimizationLoop());
        StartCoroutine(GenerateInitialChunksAndSpawnPlayer());
    }

    IEnumerator GenerateInitialChunksAndSpawnPlayer()
    {
        chunkManager.GenerateInitialChunks();

        Vector3 spawnXZ = new Vector3(0, 0, 0);
        Vector2Int spawnChunkCoord = chunkManager.GetChunkCoordFromWorldPos(spawnXZ);

        // 1. Attend que le chunk de spawn soit généré ET complètement initialisé
        while (!chunkManager.loadedChunks.ContainsKey(spawnChunkCoord))
            yield return null;

        ChunkGenerationState state;
        while (!chunkManager.ChunkStates.TryGetValue(spawnChunkCoord, out state) || state.currentStage != GenerationStage.Complete)
            yield return null;

        // 2. Attend que le MeshCollider soit prêt
        Chunk spawnChunk = chunkManager.loadedChunks[spawnChunkCoord];
        MeshCollider meshCollider = null;
        while (meshCollider == null || !meshCollider.enabled || meshCollider.sharedMesh == null)
        {
            meshCollider = spawnChunk.GetComponent<MeshCollider>();
            yield return null;
        }

        // 3. Recherche la vraie surface dans le chunk généré (du haut vers le bas)
        int localX = Mathf.FloorToInt(spawnXZ.x) - spawnChunkCoord.x * chunkManager.chunkSize;
        int localZ = Mathf.FloorToInt(spawnXZ.z) - spawnChunkCoord.y * chunkManager.chunkSize;
        int surfaceY = -1;

        // On part du haut du chunk et on descend jusqu'à trouver un bloc solide (non air, non eau)
        for (int y = chunkManager.maxWorldHeight - 1; y >= 0; y--)
        {
            BlockType block = spawnChunk.data.GetBlock(localX, y, localZ);
            if (block != BlockType.air && block != BlockType.water)
            {
                surfaceY = y;
                break;
            }
        }

        // Si on a trouvé la surface, place le joueur juste au-dessus
        if (surfaceY != -1)
            player.position = new Vector3(0, surfaceY + 1.1f, 0); // +1.1 pour être sûr d'être au-dessus
        else
            player.position = new Vector3(0, chunkManager.seaLevel + 5, 0); // fallback sécurité
    }

    IEnumerator OptimizationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            if (Time.frameCount % 300 == 0)
            {
                System.GC.Collect();
            }
        }
    }

    public float GetHeightAtPosition(float x, float z, BiomeType biomeType)
    {
        // Génération de terrain multi-octaves
        float continentNoise = Mathf.PerlinNoise(x * continentScale, z * continentScale);
        float elevationNoise = Mathf.PerlinNoise(x * elevationScale, z * elevationScale) * 0.5f;
        float ridgeNoise = Mathf.PerlinNoise(x * ridgeScale, z * ridgeScale) * 0.25f;
        float detailNoise = Mathf.PerlinNoise(x * detailScale, z * detailScale) * 0.1f;

        float combinedNoise = (continentNoise + elevationNoise + ridgeNoise + detailNoise) / 1.85f;

        var biomeData = GetBiomeData(biomeType);
        return seaLevel + biomeData.baseHeight + (combinedNoise * biomeData.heightVariation);
    }

    public BiomeType DetermineBiome(float x, float z)
    {
        float temperature = Mathf.PerlinNoise(x * temperatureScale, z * temperatureScale);
        float humidity = Mathf.PerlinNoise(x * humidityScale + 1000, z * humidityScale + 1000);
        float continentNoise = Mathf.PerlinNoise(x * continentScale, z * continentScale);

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

    // Système de blending entre biomes pour éviter les transitions abruptes [24]
    public float GetBlendedHeight(float x, float z)
    {
        // Échantillonnage de points autour de la position actuelle
        var samples = new System.Collections.Generic.Dictionary<BiomeType, float>();
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

        // Calcul de la hauteur pondérée [25]
        float blendedHeight = 0f;
        foreach (var sample in samples)
        {
            float biomeWeight = sample.Value / totalWeight;
            float biomeHeight = GetHeightForBiome(x, z, sample.Key);
            blendedHeight += biomeHeight * biomeWeight;
        }

        return blendedHeight;
    }

    // Génération de hauteur multi-octaves comme dans Minecraft [4][18]
    private float GetHeightForBiome(float x, float z, BiomeType biomeType)
    {
        // Génération de terrain en multiple octaves pour plus de réalisme [18]
        float continentNoise = Mathf.PerlinNoise(x * continentScale, z * continentScale);
        float elevationNoise = Mathf.PerlinNoise(x * elevationScale, z * elevationScale) * 0.5f;
        float ridgeNoise = Mathf.PerlinNoise(x * ridgeScale, z * ridgeScale) * 0.25f;
        float detailNoise = Mathf.PerlinNoise(x * detailScale, z * detailScale) * 0.1f;

        // Normalisation des octaves pour éviter les valeurs trop élevées [6]
        float combinedNoise = (continentNoise + elevationNoise + ridgeNoise + detailNoise) / 1.85f;

        // Application des paramètres spécifiques au biome
        var biomeData = GetBiomeData(biomeType);
        return seaLevel + biomeData.baseHeight + (combinedNoise * biomeData.heightVariation);
    }

    // Génération des couches géologiques inspirée de Minecraft [10][11]
    public BlockType GetBlockAtPosition(int x, int y, int z)
    {
        float terrainHeight = GetBlendedHeight(x, z);
        int surfaceLevel = Mathf.FloorToInt(terrainHeight);

        // Bedrock au fond du monde [10][14]
        if (y <= bedrockDepth)
            return BlockType.bedrock;

        // Air au-dessus de la surface
        if (y > surfaceLevel)
            return BlockType.air;

        // Couche de surface (herbe/sable selon le biome)
        if (y == surfaceLevel)
        {
            BiomeType biome = DetermineBiome(x, z);
            return GetSurfaceBlock(biome);
        }

        // Couches de terre (dirt) [13]
        int dirtThickness = Random.Range(minDirtThickness, maxDirtThickness + 1);
        if (y > surfaceLevel - dirtThickness)
            return BlockType.dirt;

        // Pierre (stone) pour le reste
        return BlockType.stone;
    }

    private BlockType GetSurfaceBlock(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Desert: return BlockType.sand;
            case BiomeType.Ocean: return BlockType.sand;
            default: return BlockType.grass;
        }
    }

    public Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;

        // Trouve une position de spawn sûre sur terre ferme
        for (int attempts = 0; attempts < 100; attempts++)
        {
            float x = Random.Range(-50f, 50f);
            float z = Random.Range(-50f, 50f);

            BiomeType biome = DetermineBiome(x, z);
            if (biome != BiomeType.Ocean)
            {
                float height = GetBlendedHeight(x, z);
                int surfaceLevel = Mathf.FloorToInt(height);
                spawnPos = new Vector3(x, surfaceLevel + 2f, z); // +2 pour être sûr d'être au-dessus du sol
                break;
            }
        }

        return spawnPos;
    }
}
