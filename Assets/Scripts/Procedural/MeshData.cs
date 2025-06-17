using UnityEngine;
using System.Collections.Generic;

public class MeshData
{
    private List<Vector3> vertices = new();
    private List<int> triangles = new();
    private List<Vector2> uvs = new();

    private int faceCount = 0;
    private List<Color> colors = new();


    public void AddFace(BlockType type, int x, int y, int z, Vector3Int direction)
    {
        Vector3[] faceVertices = VoxelData.GetFaceVertices(direction);
        foreach (var v in faceVertices)
            vertices.Add(new Vector3(x, y, z) + v);

        int vIndex = faceCount * 4;
        triangles.AddRange(new int[] { vIndex, vIndex + 1, vIndex + 2, vIndex, vIndex + 2, vIndex + 3 });

        // UVs
        Rect rect = GetUVRectFromAtlas(type, direction);
        if (direction == Vector3Int.left || direction == Vector3Int.right ||
            direction == Vector3Int.forward || direction == Vector3Int.back)
        {
            uvs.AddRange(GetFaceUVsRotated(rect)); // côtés → rotation
        }
        else
        {
            uvs.AddRange(GetFaceUVs(rect)); // haut et bas → normal
        }

        // Colors
        Color color = Color.white;
        if (type == BlockType.Grass && direction == Vector3Int.up)
            color = new Color(0.4f, 0.85f, 0.4f, 1f); // herbe top

        for (int i = 0; i < 4; i++)
            colors.Add(color);

        faceCount++;
    }

    private Vector2[] GetFaceUVsRotated(Rect rect)
    {
        return new Vector2[]
        {
        new Vector2(rect.xMax, rect.yMin),
        new Vector2(rect.xMax, rect.yMax),
        new Vector2(rect.xMin, rect.yMax),
        new Vector2(rect.xMin, rect.yMin)
        };
    }


    private Rect GetUVRectFromAtlas(BlockType type, Vector3Int direction)
    {
        string name = "dirt.png";

        if (type == BlockType.Grass)
        {
            if (direction == Vector3Int.up)
                name = "grass_block_top.png";
            else if (direction == Vector3Int.down)
                name = "dirt.png";
            else
                name = "grass_block_side.png";
        }
        else if (type == BlockType.Stone)
            name = "stone.png";
        else if (type == BlockType.Sand)
            name = "sand.png";
        else if (type == BlockType.Dirt)
            name = "dirt.png";

        return TextureAtlasManager.instance.GetUV(name);
    }

    private Vector2[] GetFaceUVs(Rect rect)
    {
        return new Vector2[]
        {
        new Vector2(rect.xMin, rect.yMin),
        new Vector2(rect.xMax, rect.yMin),
        new Vector2(rect.xMax, rect.yMax),
        new Vector2(rect.xMin, rect.yMax),
        };
    }


    public Mesh ToMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };
        mesh.colors = colors.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}