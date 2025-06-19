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
    public GameObject loadingScreen; // Référence à l'écran de chargement
    public SaveManager saveManager;

    void Start()
    {
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

            // Charger la seed du monde
            int loadedSeed = saveManager.LoadWorldSeed();
            if (loadedSeed != -1)
            {
                worldSeed = loadedSeed;
            }
        }

        // Initialiser le générateur de nombres aléatoires avec la seed
        Random.InitState(worldSeed);

        StartCoroutine(OptimizationLoop());
        StartCoroutine(GenerateInitialChunksAndSpawnPlayer());
    }

    IEnumerator GenerateInitialChunksAndSpawnPlayer()
    {
        // Activez l'écran de chargement
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // 1. Génère les chunks autour de (0,0)
        chunkManager.GenerateInitialChunks();

        Vector3 spawnXZ = new Vector3(0, 0, 0);
        Vector2Int spawnChunkCoord = chunkManager.GetChunkCoordFromWorldPos(spawnXZ);

        // 2. Attend que le chunk de spawn soit chargé
        while (!chunkManager.loadedChunks.ContainsKey(spawnChunkCoord))
            yield return null;

        // 3. Attend que la génération soit complète
        ChunkGenerationState state;
        while (!chunkManager.ChunkStates.TryGetValue(spawnChunkCoord, out state) || state.currentStage != GenerationStage.Complete)
            yield return null;

        // 4. Attend que le MeshCollider soit prêt
        Chunk spawnChunk = chunkManager.loadedChunks[spawnChunkCoord];
        MeshCollider meshCollider = null;
        while (meshCollider == null || !meshCollider.enabled || meshCollider.sharedMesh == null)
        {
            meshCollider = spawnChunk.GetComponent<MeshCollider>();
            yield return null;
        }

        // 5. Trouve la hauteur du sol à cette position
        int localX = Mathf.FloorToInt(spawnXZ.x) - spawnChunkCoord.x * chunkManager.chunkSize;
        int localZ = Mathf.FloorToInt(spawnXZ.z) - spawnChunkCoord.y * chunkManager.chunkSize;
        int surfaceY = -1;

        for (int y = maxWorldHeight - 1; y >= 0; y--)
        {
            BlockType block = spawnChunk.data.GetBlock(localX, y, localZ);
            if (block != BlockType.air && block != BlockType.water)
            {
                surfaceY = y;
                break;
            }
        }

        // 6. Charge la position du joueur
        Vector3 savedPlayerPosition = saveManager.LoadPlayerPosition();

        // 7. Téléporte le joueur à la position sauvegardée ou juste au-dessus du sol
        float finalY = (surfaceY != -1) ? surfaceY + 1.1f : chunkManager.proceduralWorldManager.seaLevel + 5f;
        Vector3 spawnPosition = new Vector3(savedPlayerPosition.x, finalY, savedPlayerPosition.z);
        player.GetComponent<PlayerController>().TeleportTo(spawnPosition);

        // Désactivez l'écran de chargement
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }

        // ⬇️ Ajout du chargement du joueur
        if (saveManager != null)
        {
            saveManager.LoadPlayer();
        }
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

    // Système de blending entre biomes pour éviter les transitions abruptes
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

        // Calcul de la hauteur pondérée
        float blendedHeight = 0f;
        foreach (var sample in samples)
        {
            float biomeWeight = sample.Value / totalWeight;
            float biomeHeight = GetHeightForBiome(x, z, sample.Key);
            blendedHeight += biomeHeight * biomeWeight;
        }

        return blendedHeight;
    }

    // Génération de hauteur multi-octaves comme dans Minecraft
    private float GetHeightForBiome(float x, float z, BiomeType biomeType)
    {
        // Génération de terrain en multiple octaves pour plus de réalisme
        float continentNoise = Mathf.PerlinNoise(x * continentScale + worldSeed, z * continentScale + worldSeed);
        float elevationNoise = Mathf.PerlinNoise(x * elevationScale + worldSeed, z * elevationScale + worldSeed) * 0.5f;
        float ridgeNoise = Mathf.PerlinNoise(x * ridgeScale + worldSeed, z * ridgeScale + worldSeed) * 0.25f;
        float detailNoise = Mathf.PerlinNoise(x * detailScale + worldSeed, z * detailScale + worldSeed) * 0.1f;

        // Normalisation des octaves pour éviter les valeurs trop élevées
        float combinedNoise = (continentNoise + elevationNoise + ridgeNoise + detailNoise) / 1.85f;

        // Application des paramètres spécifiques au biome
        var biomeData = GetBiomeData(biomeType);
        return seaLevel + biomeData.baseHeight + (combinedNoise * biomeData.heightVariation);
    }

    // Génération des couches géologiques inspirée de Minecraft
    public BlockType GetBlockAtPosition(int x, int y, int z)
    {
        float terrainHeight = GetBlendedHeight(x, z);
        int surfaceLevel = Mathf.FloorToInt(terrainHeight);

        // Bedrock au fond du monde
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

        // Couches de terre (dirt)
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

    void OnApplicationQuit()
    {
        if (saveManager != null)
        {
            saveManager.SavePlayer();
            saveManager.SavePlayerPosition();
        }
    }
}
