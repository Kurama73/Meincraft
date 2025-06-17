using UnityEngine;
using System.Collections.Generic;

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

    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    private Vector2Int lastPlayerChunk = Vector2Int.zero;
    private Camera playerCamera;

    public Material chunkMaterial;

    void Start()
    {
        playerCamera = Camera.main;
        if (player == null) player = FindObjectOfType<PlayerController>().transform;

        GenerateInitialChunks();
    }

    void Update()
    {
        Vector2Int currentPlayerChunk = GetChunkCoordFromWorldPos(player.position);

        // Log du biome actuel à la position du joueur
        Biome currentBiome = proceduralWorldManager.DetermineBiome(player.position.x, player.position.z);
        Debug.Log($"Biome: {currentBiome.type}");

        if (Vector2Int.Distance(currentPlayerChunk, lastPlayerChunk) > updateDistance)
        {
            UpdateChunks();
            lastPlayerChunk = currentPlayerChunk;
        }

        UpdateChunkVisibility();
    }

    void GenerateInitialChunks()
    {
        Vector2Int playerChunk = GetChunkCoordFromWorldPos(player.position);

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                CreateChunk(chunkCoord);
            }
        }
    }

    void UpdateChunks()
    {
        Vector2Int playerChunk = GetChunkCoordFromWorldPos(player.position);

        List<Vector2Int> chunksToRemove = new List<Vector2Int>();
        foreach (var chunk in loadedChunks)
        {
            if (Vector2Int.Distance(chunk.Key, playerChunk) > viewDistance + 2)
            {
                chunksToRemove.Add(chunk.Key);
            }
        }

        foreach (var coord in chunksToRemove)
        {
            DestroyChunk(coord);
        }

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    CreateChunk(chunkCoord);
                }
            }
        }
    }

    void UpdateChunkVisibility()
    {
        foreach (var chunk in loadedChunks.Values)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Bounds chunkBounds = new Bounds(
                    chunk.gameObject.transform.position + Vector3.one * chunkSize * 0.5f,
                    Vector3.one * chunkSize
                );

                bool isVisible = GeometryUtility.TestPlanesAABB(
                    GeometryUtility.CalculateFrustumPlanes(playerCamera),
                    chunkBounds
                );

                chunk.gameObject.SetActive(isVisible);
            }
        }
    }

    void CreateChunk(Vector2Int coord)
    {
        if (loadedChunks.ContainsKey(coord)) return;

        GameObject chunkObject = new GameObject($"Chunk_{coord.x}_{coord.y}");
        chunkObject.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        int chunkLayer = LayerMask.NameToLayer("Chunk");
        if (chunkLayer == -1)
        {
            Debug.LogWarning("Layer 'Chunk' is not defined. Using default layer.");
            chunkLayer = 0;
        }
        chunkObject.layer = chunkLayer;

        var meshFilter = chunkObject.AddComponent<MeshFilter>();
        var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        var meshCollider = chunkObject.AddComponent<MeshCollider>();

        meshRenderer.material = chunkMaterial;

        Chunk newChunk = chunkObject.AddComponent<Chunk>();
        Biome biome = proceduralWorldManager.DetermineBiome(coord.x * chunkSize, coord.y * chunkSize);

        newChunk.Initialize(coord, chunkSize, (x, y, z) => {
            int height = Mathf.FloorToInt(proceduralWorldManager.GetHeightAtPosition(x, z, biome));

            if (y <= height)
            {
                if (y == height && biome.type == BiomeType.Forest && Random.value < 0.1f)
                {
                    new TreeGenerator().GenerateTree(new Vector3(x, y + 1, z));
                }
                return BlockType.grass;
            }
            else
            {
                return BlockType.air;
            }
        });

        loadedChunks.Add(coord, newChunk);
    }




    void DestroyChunk(Vector2Int coord)
    {
        if (loadedChunks.ContainsKey(coord))
        {
            if (loadedChunks[coord] != null)
            {
                Destroy(loadedChunks[coord].gameObject);
            }
            loadedChunks.Remove(coord);
        }
    }

    Vector2Int GetChunkCoordFromWorldPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
}
