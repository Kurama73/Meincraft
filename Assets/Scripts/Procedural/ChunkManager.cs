using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Settings")]
    public int chunkSize = 16;
    public int viewDistance = 8;
    public float updateDistance = 5f;

    [Header("Block Settings")]
    public BlockGenerator blockGenerator;

    [Header("Player")]
    public Transform player;

    [Header("References")]
    public ProceduralWorldManager proceduralWorldManager;

    [Header("Ore Generation")]
    public float coalFrequency = 0.02f;
    public float ironFrequency = 0.015f;
    public float goldFrequency = 0.008f;
    public float diamondFrequency = 0.003f;

    public Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int lastPlayerChunk = Vector2Int.zero;
    private Camera playerCamera;
    public float chunkViewAngle = 200f;
    public Material chunkMaterial;

    private Dictionary<Vector2Int, ChunkGenerationState> chunkStates = new Dictionary<Vector2Int, ChunkGenerationState>();
    private Queue<Vector2Int> generationQueue = new Queue<Vector2Int>();
    private List<Vector2Int> chunksBeingGenerated = new List<Vector2Int>();
    private Queue<Vector2Int> chunkGenerationQueue = new Queue<Vector2Int>();
    private bool isChunkGenerationRunning = false;
    public int maxChunksPerFrame = 12;
    public Dictionary<Vector2Int, ChunkGenerationState> ChunkStates => chunkStates;

    // --- GÉNÉRATION MULTI-ÉTAPES ---
    public IEnumerator CreateChunkStaged(Vector2Int coord)
    {
        if (loadedChunks.ContainsKey(coord)) yield break;

        // Création de l'état de génération
        ChunkGenerationState state = new ChunkGenerationState(coord);
        chunkStates[coord] = state;
        state.isBeingGenerated = true;
        chunksBeingGenerated.Add(coord);

        GameObject chunkObject = CreateChunkGameObject(coord);
        Chunk newChunk = chunkObject.AddComponent<Chunk>();
        loadedChunks.Add(coord, newChunk);

        // Étapes de génération séquentielles
        yield return StartCoroutine(GenerateTerrainStage(newChunk, state));
        yield return StartCoroutine(GenerateCavesStage(newChunk, state));
        yield return StartCoroutine(GenerateDecorationsStage(newChunk, state));
        yield return StartCoroutine(GenerateTreesStage(newChunk, state));

        // Ajoute d'autres étapes ici si besoin (rivières, structures, ressources...)
        state.currentStage = GenerationStage.Complete;
        chunksBeingGenerated.Remove(coord);
    }

    // --- STUBS POUR LES ÉTAPES DE GÉNÉRATION ---
    private IEnumerator GenerateTerrainStage(Chunk chunk, ChunkGenerationState state)
    {
        chunk.Initialize(state.coord, chunkSize, (localX, y, localZ) =>
        {
            // Conversion en coordonnées monde
            int worldX = state.coord.x * chunkSize + localX;
            int worldZ = state.coord.y * chunkSize + localZ;

            // Utilisation du nouveau système de génération
            return proceduralWorldManager.GetBlockAtPosition(worldX, y, worldZ);
        });

        state.currentStage = GenerationStage.Terrain;
        yield return null;
    }

    private IEnumerator GenerateCavesStage(Chunk chunk, ChunkGenerationState state)
    {
        // À implémenter selon ton système de grottes
        state.currentStage = GenerationStage.Caves;
        yield return null;
    }

    private IEnumerator GenerateDecorationsStage(Chunk chunk, ChunkGenerationState state)
    {
        // À implémenter pour fleurs, buissons, etc.
        state.currentStage = GenerationStage.Decorations;
        yield return null;
    }

    private IEnumerator GenerateTreesStage(Chunk chunk, ChunkGenerationState state)
    {
        BiomeType biomeType = proceduralWorldManager.DetermineBiome(state.coord.x * chunkSize, state.coord.y * chunkSize);
        Biome biome = GetBiomeFromType(biomeType);

        if (biome.type == BiomeType.Forest)
        {
            yield return StartCoroutine(GenerateTreesForChunk(state.coord, biome));
        }

        state.currentStage = GenerationStage.Trees;
        yield return null;
    }

    // --- GÉNÉRATION D'ARBRES ---
    IEnumerator GenerateTreesForChunk(Vector2Int coord, Biome biome)
    {
        if (biome.type != BiomeType.Forest) yield break;

        int treesToGenerate = Random.Range(2, 5);
        float[,] noiseMap = new float[chunkSize, chunkSize];

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                noiseMap[x, z] = Mathf.PerlinNoise(
                    (coord.x * chunkSize + x) * 0.8f,
                    (coord.y * chunkSize + z) * 0.8f
                );
            }
        }

        for (int i = 0; i < treesToGenerate; i++)
        {
            int x = Random.Range(0, chunkSize);
            int z = Random.Range(0, chunkSize);

            if (noiseMap[x, z] > 0.6f)
            {
                int worldX = coord.x * chunkSize + x;
                int worldZ = coord.y * chunkSize + z;
                int height = Mathf.FloorToInt(proceduralWorldManager.GetHeightAtPosition(worldX, worldZ, biome.type));
                GenerateSingleTree(new Vector3(worldX, height + 1, worldZ));
                yield return null;
            }
        }
    }

    void GenerateSingleTree(Vector3 position)
    {
        var treeGen = new TreeGenerator();
        treeGen.chunkManager = this;
        treeGen.GenerateTree(position);
    }

    // --- UTILS ---
    private GameObject CreateChunkGameObject(Vector2Int coord)
    {
        GameObject chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObject.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        int chunkLayer = LayerMask.NameToLayer("Chunk");
        if (chunkLayer == -1)
        {
            Debug.LogWarning("Layer 'Chunk' is not defined. Using default layer.");
            chunkLayer = 0;
        }

        chunkObject.layer = chunkLayer;
        chunkObject.AddComponent<MeshFilter>();
        chunkObject.AddComponent<MeshRenderer>();
        chunkObject.AddComponent<MeshCollider>();
        chunkObject.GetComponent<MeshRenderer>().material = chunkMaterial;

        return chunkObject;
    }

    // Méthode utilitaire pour obtenir un Biome complet à partir d'un BiomeType
    private Biome GetBiomeFromType(BiomeType type)
    {
        // À adapter selon ta base de données de biomes
        switch (type)
        {
            case BiomeType.Forest: return new Biome(BiomeType.Forest, 0, 10, Color.green);
            case BiomeType.Mountain: return new Biome(BiomeType.Mountain, 10, 20, Color.gray);
            case BiomeType.Desert: return new Biome(BiomeType.Desert, 5, 3, Color.yellow);
            case BiomeType.Ocean: return new Biome(BiomeType.Ocean, -5, 2, Color.blue);
            default: return new Biome(BiomeType.Plains, 0, 5, Color.green);
        }
    }

    // --- GESTION DES CHUNKS ---
    void Start()
    {
        playerCamera = Camera.main;
        if (player == null) player = FindObjectOfType<PlayerController>().transform;
        GenerateInitialChunks();
    }

    void Update()
    {
        Vector2Int currentPlayerChunk = GetChunkCoordFromWorldPos(player.position);

        BiomeType biomeType = proceduralWorldManager.DetermineBiome(player.position.x, player.position.z);
        Biome currentBiome = GetBiomeFromType(biomeType);

        if (Vector2Int.Distance(currentPlayerChunk, lastPlayerChunk) > updateDistance)
        {
            UpdateChunks();
            lastPlayerChunk = currentPlayerChunk;
        }

        UpdateChunkVisibility();
    }

    public void GenerateInitialChunks()
    {
        int generationRadius = viewDistance * 2;
        Vector2Int playerChunk = GetChunkCoordFromWorldPos(player.position);

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                if (Vector2Int.Distance(chunkCoord, playerChunk) <= viewDistance)
                {
                    if (!loadedChunks.ContainsKey(chunkCoord) && !chunkGenerationQueue.Contains(chunkCoord))
                    {
                        chunkGenerationQueue.Enqueue(chunkCoord);
                    }
                }
            }
        }

        if (!isChunkGenerationRunning)
            StartCoroutine(ProcessChunkGenerationQueue());
    }

    void UpdateChunks()
    {
        int generationRadius = viewDistance * 2;
        Vector2Int playerChunk = GetChunkCoordFromWorldPos(player.position);

        // 1. Détermine les chunks à garder/générer
        HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                if (Vector2Int.Distance(chunkCoord, playerChunk) <= generationRadius)
                {
                    neededChunks.Add(chunkCoord);
                    if (!loadedChunks.ContainsKey(chunkCoord) && !chunkGenerationQueue.Contains(chunkCoord))
                    {
                        chunkGenerationQueue.Enqueue(chunkCoord);
                    }
                }
            }
        }

        // 2. Détermine les chunks à décharger
        List<Vector2Int> chunksToRemove = new List<Vector2Int>();

        foreach (var chunk in loadedChunks)
        {
            if (!neededChunks.Contains(chunk.Key))
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            DestroyChunk(coord);
        }

        // Lance la génération si ce n'est pas déjà en cours
        if (!isChunkGenerationRunning)
            StartCoroutine(ProcessChunkGenerationQueue());
    }

    private IEnumerator ProcessChunkGenerationQueue()
    {
        isChunkGenerationRunning = true;

        while (chunkGenerationQueue.Count > 0)
        {
            int generatedThisFrame = 0;

            while (generatedThisFrame < maxChunksPerFrame && chunkGenerationQueue.Count > 0)
            {
                Vector2Int coord = chunkGenerationQueue.Dequeue();
                if (!loadedChunks.ContainsKey(coord))
                {
                    yield return StartCoroutine(CreateChunkStaged(coord));
                }
                generatedThisFrame++;
            }

            // Attend la fin de la frame pour ne pas bloquer le jeu
            yield return null;
        }

        isChunkGenerationRunning = false;
    }

    void UpdateChunkVisibility()
    {
        if (player == null)
            return;

        Vector3 playerPos = player.position;
        Vector2 playerPos2D = new Vector2(playerPos.x, playerPos.z);

        foreach (var kvp in loadedChunks)
        {
            Vector2Int chunkCoord = kvp.Key;
            Chunk chunk = kvp.Value;

            Vector2 chunkCenter2D = new Vector2(
                chunkCoord.x * chunkSize + chunkSize / 2f,
                chunkCoord.y * chunkSize + chunkSize / 2f
            );

            float dist = Vector2.Distance(playerPos2D, chunkCenter2D);

            // Affiche le chunk s'il est dans le rayon
            bool visible = dist <= chunkSize * viewDistance;
            chunk.gameObject.SetActive(visible);
        }
    }

    public void DestroyChunk(Vector2Int coord)
    {
        if (loadedChunks.ContainsKey(coord))
        {
            if (loadedChunks[coord] != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(loadedChunks[coord].gameObject);
                else
                    Destroy(loadedChunks[coord].gameObject);
#else
                    Destroy(loadedChunks[coord].gameObject);
#endif
            }
            loadedChunks.Remove(coord);
        }
    }

    /// <summary>
    /// Supprime tous les chunks chargés dans la scène.
    /// </summary>
    public void ClearAllChunks()
    {
        var coords = new List<Vector2Int>(loadedChunks.Keys);
        foreach (var coord in coords)
        {
            DestroyChunk(coord);
        }
        loadedChunks.Clear();
        chunkStates.Clear();
        chunksBeingGenerated.Clear();
        generationQueue.Clear();
    }

    public Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }

    // Méthode publique pour placer un bloc dans le monde
    public void SetBlock(int x, int y, int z, BlockType type)
    {
        Vector3 worldPos = new Vector3(x, y, z);
        Vector2Int chunkCoord = GetChunkCoordFromWorldPos(worldPos);
        if (loadedChunks.TryGetValue(chunkCoord, out Chunk chunk) && chunk != null)
        {
            chunk.SetBlock(worldPos, type);
        }
    }

    void OnApplicationQuit()
    {
        SaveAllChunks();
    }

    public void SaveAllChunks()
    {
        foreach (var kvp in loadedChunks)
        {
            if (kvp.Value != null)
            {
                ChunkSaveSystem.SaveChunkData(kvp.Value.data);
            }
        }
        Debug.Log("Tous les chunks actifs ont été sauvegardés.");
    }
}
