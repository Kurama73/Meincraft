using UnityEngine;
using Unity.Collections;
using System.Collections;
using Unity.Jobs;
using Unity.Burst;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;
using System;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector2Int coord;
    public ChunkData data;
    public int size;

    private Mesh mesh;
    public NativeArray<BlockType> blockDataNative;
    private JobHandle generationHandle;

    void OnDisable()
    {
        if (blockDataNative.IsCreated)
            blockDataNative.Dispose();
    }

    // Initialisation asynchrone via Job System
    public void InitializeAsync(Vector2Int coord, int size, int worldSeed,
                                float continentScale, float elevationScale)
    {
        this.coord = coord;
        this.size = size;
        data = new ChunkData(coord, size);
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        int totalVoxels = size * size;
        blockDataNative = new NativeArray<BlockType>(size * 64 * size, Allocator.Persistent);

        // Planification du job
        var job = new TerrainGenJob
        {
            chunkSize = size,
            worldSeed = worldSeed,
            continentScale = continentScale,
            elevationScale = elevationScale,
            blockData = blockDataNative
        };
        generationHandle = job.Schedule();

        StartCoroutine(WaitForJobAndBuildMesh());
    }

    public void InitializeFromJob(NativeArray<BlockType> blockData, int maxHeight)
    {
        data = new ChunkData(coord, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < maxHeight; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    int idx = x + size * (z + size * y);
                    data.SetBlock(x, y, z, blockData[idx]);
                }
            }
        }
        GenerateMesh();
    }



    private IEnumerator WaitForJobAndBuildMesh()
    {
        yield return new WaitUntil(() => generationHandle.IsCompleted);
        generationHandle.Complete();

        // Copie des données dans data
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                for (int y = 0; y < 64; y++)
                {
                    int i = x + z * size + y * size * size;
                    data.SetBlock(x, y, z, blockDataNative[i]);
                }

        GenerateMesh();
        blockDataNative.Dispose();
    }

    public void Initialize(Vector2Int coord, int size, Func<int, int, int, BlockType> gen)
    {
        this.coord = coord;
        this.size = size;
        data = new ChunkData(coord, size);
        for (int x = 0; x < size; x++)
            for (int y = 0; y < 64; y++)
                for (int z = 0; z < size; z++)
                    data.SetBlock(x, y, z, gen(x, y, z));
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        MeshData meshData = new MeshData();
        for (int x = 0; x < size; x++)
            for (int y = 0; y < 64; y++)
                for (int z = 0; z < size; z++)
                {
                    BlockType block = data.GetBlock(x, y, z);
                    if (block != BlockType.air)
                    {
                        // Correction: Utilisation de Vector3Int au lieu de Vector3
                        if (data.GetBlock(x, y + 1, z) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.up);
                        if (data.GetBlock(x, y - 1, z) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.down);
                        if (data.GetBlock(x + 1, y, z) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.right);
                        if (data.GetBlock(x - 1, y, z) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.left);
                        if (data.GetBlock(x, y, z + 1) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.forward);
                        if (data.GetBlock(x, y, z - 1) == BlockType.air)
                            meshData.AddFace(block, x, y, z, Vector3Int.back);
                    }
                }
        Mesh m = meshData.ToMesh();
        GetComponent<MeshFilter>().mesh = m;
        GetComponent<MeshCollider>().sharedMesh = m;
    }

    public void SetBlock(Vector3 worldPos, BlockType newType)
    {
        int localX = Mathf.FloorToInt(worldPos.x - transform.position.x);
        int y = Mathf.FloorToInt(worldPos.y);
        int localZ = Mathf.FloorToInt(worldPos.z - transform.position.z);
        if (localX < 0 || localZ < 0 || localX >= size || localZ >= size || y < 0 || y >= 64)
            return;
        data.SetBlock(localX, y, localZ, newType);
        Refresh();
    }

    // Nouvelle méthode pour TreeGenerator
    public void SetBlockWithoutRefresh(Vector3 worldPos, BlockType newType)
    {
        int localX = Mathf.FloorToInt(worldPos.x - transform.position.x);
        int y = Mathf.FloorToInt(worldPos.y);
        int localZ = Mathf.FloorToInt(worldPos.z - transform.position.z);
        if (IsValidPosition(localX, y, localZ))
        {
            data.SetBlock(localX, y, localZ, newType);
        }
    }

    private bool IsValidPosition(int x, int y, int z)
    {
        return x >= 0 && x < size &&
               y >= 0 && y < 64 &&
               z >= 0 && z < size;
    }

    public void Refresh()
    {
        GenerateMesh();
        ChunkSaveSystem.SaveChunkData(data);
    }

    // Alias pour RefreshChunk
    public void RefreshChunk() => Refresh();
}
