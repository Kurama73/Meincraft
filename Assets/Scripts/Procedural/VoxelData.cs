using UnityEngine;

public static class VoxelData
{
    public static readonly Vector3Int[] directions =
    {
        new Vector3Int(0, 1, 0), // top
        new Vector3Int(0, -1, 0), // bottom
        new Vector3Int(0, 0, 1), // front
        new Vector3Int(0, 0, -1), // back
        new Vector3Int(1, 0, 0), // right
        new Vector3Int(-1, 0, 0), // left
    };

    public static Vector3[] GetFaceVertices(Vector3Int dir)
    {
        Vector3 p0 = Vector3.zero, p1 = Vector3.zero, p2 = Vector3.zero, p3 = Vector3.zero;

        if (dir == Vector3Int.up)
        {
            p0 = new(0, 1, 0); p1 = new(0, 1, 1); p2 = new(1, 1, 1); p3 = new(1, 1, 0);
        }
        else if (dir == Vector3Int.down)
        {
            p0 = new(0, 0, 0); p1 = new(1, 0, 0); p2 = new(1, 0, 1); p3 = new(0, 0, 1);
        }
        else if (dir == Vector3Int.forward)
        {
            p0 = new(1, 0, 1); p1 = new(1, 1, 1); p2 = new(0, 1, 1); p3 = new(0, 0, 1);
        }
        else if (dir == Vector3Int.back)
        {
            p0 = new(0, 0, 0); p1 = new(0, 1, 0); p2 = new(1, 1, 0); p3 = new(1, 0, 0);
        }
        else if (dir == Vector3Int.right)
        {
            p0 = new(1, 0, 0); p1 = new(1, 1, 0); p2 = new(1, 1, 1); p3 = new(1, 0, 1);
        }
        else if (dir == Vector3Int.left)
        {
            p0 = new(0, 0, 1); p1 = new(0, 1, 1); p2 = new(0, 1, 0); p3 = new(0, 0, 0);
        }

        return new Vector3[] { p0, p1, p2, p3 };
    }
}