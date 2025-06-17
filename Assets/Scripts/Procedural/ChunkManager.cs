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

        // Vérifier si le joueur a changé de chunk
        if (Vector2Int.Distance(currentPlayerChunk, lastPlayerChunk) > updateDistance)
        {
            UpdateChunks();
            lastPlayerChunk = currentPlayerChunk;
        }

        // Frustum culling pour les chunks visibles
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

        // Supprimer les chunks trop éloignés
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

        // Créer de nouveaux chunks
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
                // Vérifier si le chunk est dans le champ de vision
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

        // Affecte la couche
        int chunkLayer = LayerMask.NameToLayer("Chunk");
        if (chunkLayer == -1)
        {
            Debug.LogWarning("Layer 'Chunk' is not defined. Using default layer.");
            chunkLayer = 0; // Couche par défaut
        }
        chunkObject.layer = chunkLayer;

        // Ajoute les composants nécessaires
        var meshFilter = chunkObject.AddComponent<MeshFilter>();
        var meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        var meshCollider = chunkObject.AddComponent<MeshCollider>();

        // Applique le matériel de l’atlas
        meshRenderer.material = chunkMaterial;

        // Ajoute le script Chunk et initialise
        Chunk newChunk = chunkObject.AddComponent<Chunk>();
        newChunk.Initialize(coord, chunkSize, (x, y, z) => {
            float noise = Mathf.PerlinNoise((coord.x * chunkSize + x) * 0.05f, (coord.y * chunkSize + z) * 0.05f);
            int height = Mathf.FloorToInt(noise * 10f);
            return y <= height ? BlockType.Grass : BlockType.Air;
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
