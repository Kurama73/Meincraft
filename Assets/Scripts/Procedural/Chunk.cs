using UnityEngine;
using static UnityEngine.Mesh;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector2Int coord;
    public ChunkData data;
    public int size;

    public void Initialize(Vector2Int coord, int size, System.Func<int, int, int, BlockType> generator)
    {
        this.coord = coord;
        this.size = size;

        if (ChunkSaveSystem.ChunkExists(coord))
        {
            data = ChunkSaveSystem.LoadChunkData(coord);
            if (data == null)
            {
                Debug.LogError("Failed to load chunk data, generating new chunk.");
                data = new ChunkData(coord, size, generator);
            }
        }
        else
        {
            data = new ChunkData(coord, size, generator);
        }

        if (data != null)
        {
            GenerateMesh();
        }
        else
        {
            Debug.LogError("Failed to initialize chunk data.");
        }
    }

    void GenerateMesh()
    {
        if (data == null)
        {
            Debug.LogError("Chunk data is not initialized.");
            return;
        }

        MeshData meshData = new MeshData();

        for (int x = 0; x < size; x++)
            for (int y = 0; y < 64; y++)
                for (int z = 0; z < size; z++)
                {
                    BlockType block = data.GetBlock(x, y, z);
                    if (block == BlockType.Air) continue;

                    foreach (var dir in VoxelData.directions)
                    {
                        int nx = x + dir.x;
                        int ny = y + dir.y;
                        int nz = z + dir.z;

                        if (data.GetBlock(nx, ny, nz) == BlockType.Air)
                            meshData.AddFace(block, x, y, z, dir);
                    }
                }

        Mesh mesh = meshData.ToMesh();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void SetBlock(Vector3 worldPos, BlockType newType)
    {
        int localX = Mathf.FloorToInt(worldPos.x - transform.position.x);
        int y = Mathf.FloorToInt(worldPos.y);
        int localZ = Mathf.FloorToInt(worldPos.z - transform.position.z);

        if (localX < 0 || localZ < 0 || localX >= size || localZ >= size || y < 0 || y >= 64)
            return;

        data.SetBlock(localX, y, localZ, newType);
        ChunkSaveSystem.SaveChunkData(data);
        GenerateMesh();
    }
}
