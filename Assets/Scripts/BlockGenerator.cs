using UnityEngine;

public class BlockGenerator : MonoBehaviour
{
    public TextureAtlasManager atlasManager;
    public Material blockMaterial;

    // Génère des UVs avec tous les modes bien clairs
    Vector2[] GetUVs(UnityEngine.Rect rect, UVMode mode = UVMode.Normal)
    {
        Vector2[] uvs = new Vector2[4];
        switch (mode)
        {
            case UVMode.Inverted: // Face du dessus
                uvs[0] = new Vector2(rect.xMin, rect.yMax);
                uvs[1] = new Vector2(rect.xMax, rect.yMax);
                uvs[2] = new Vector2(rect.xMax, rect.yMin);
                uvs[3] = new Vector2(rect.xMin, rect.yMin);
                break;

            case UVMode.FlipY: // Dessous
                uvs[0] = new Vector2(rect.xMin, rect.yMax);
                uvs[1] = new Vector2(rect.xMax, rect.yMax);
                uvs[2] = new Vector2(rect.xMax, rect.yMin);
                uvs[3] = new Vector2(rect.xMin, rect.yMin);
                break;

            case UVMode.Rotate90: // Côtés
                uvs[0] = new Vector2(rect.xMin, rect.yMin);
                uvs[1] = new Vector2(rect.xMin, rect.yMax);
                uvs[2] = new Vector2(rect.xMax, rect.yMax);
                uvs[3] = new Vector2(rect.xMax, rect.yMin);
                break;

            case UVMode.Normal:
            default:
                uvs[0] = new Vector2(rect.xMin, rect.yMin);
                uvs[1] = new Vector2(rect.xMax, rect.yMin);
                uvs[2] = new Vector2(rect.xMax, rect.yMax);
                uvs[3] = new Vector2(rect.xMin, rect.yMax);
                break;
        }
        return uvs;
    }

    enum UVMode { Normal, FlipY, Rotate90, Inverted }

    public GameObject CreateBlock(Vector3 pos, string top, string bottom, string side)
    {
        GameObject go = new GameObject("Block");
        go.transform.position = pos;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.material = blockMaterial;
        go.AddComponent<BoxCollider>();

        Mesh mesh = new Mesh();

        Vector3[] verts = new Vector3[]
        {
            // Haut (Y+)
            new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(0, 1, 1),
            // Bas (Y-)
            new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 0, 0),
            // Avant (Z+)
            new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 0, 1),
            // Arrière (Z-)
            new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0),
            // Droite (X+)
            new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(1, 0, 0),
            // Gauche (X-)
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(0, 0, 1)
        };

        int[] tris = new int[]
        {
            // Haut (Y+)
            0, 2, 1, 0, 3, 2,
            // Bas (Y-)
            4, 6, 5, 4, 7, 6,
            // Avant (Z+)
            8, 10, 9, 8, 11, 10,
            // Arrière (Z-)
            12, 14, 13, 12, 15, 14,
            // Droite (X+)
            16, 18, 17, 16, 19, 18,
            // Gauche (X-)
            20, 22, 21, 20, 23, 22
        };

        // Génère les UVs
        UnityEngine.Rect rectTop = atlasManager.GetUV(top);
        UnityEngine.Rect rectBottom = atlasManager.GetUV(bottom);
        UnityEngine.Rect rectSide = atlasManager.GetUV(side);

        Vector2[] meshUVs = new Vector2[24];
        GetUVs(rectTop, UVMode.Inverted).CopyTo(meshUVs, 0);     // Haut
        GetUVs(rectBottom, UVMode.FlipY).CopyTo(meshUVs, 4);     // Bas
        GetUVs(rectSide, UVMode.Rotate90).CopyTo(meshUVs, 8);    // Avant
        GetUVs(rectSide, UVMode.Rotate90).CopyTo(meshUVs, 12);   // Arrière
        GetUVs(rectSide, UVMode.Rotate90).CopyTo(meshUVs, 16);   // Droite
        GetUVs(rectSide, UVMode.Rotate90).CopyTo(meshUVs, 20);   // Gauche

        // Ajoute les vertex colors : vert pour le dessus si grass_block_top, blanc sinon
        Color[] meshColors = new Color[24];
        for (int i = 0; i < 24; i++) meshColors[i] = Color.white; // par défaut

        // Les 4 premiers sommets sont le dessus (Y+)
        if (top == "grass_block_top.png")
        {
            meshColors[0] = meshColors[1] = meshColors[2] = meshColors[3] = new Color(0.4f, 0.85f, 0.4f, 1f);
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = meshUVs;
        mesh.colors = meshColors; // C'est ça qui colore vraiment
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        return go;
    }
}
