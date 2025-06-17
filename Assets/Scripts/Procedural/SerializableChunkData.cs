using System;
using UnityEngine;

[System.Serializable]
public class SerializableChunkData
{
    public int[] coord; // Utilisez un tableau d'entiers pour stocker les coordonnées
    public int[,,] blocks; // Utilisez un tableau d'entiers pour stocker les types de blocs

    public SerializableChunkData(ChunkData data)
    {
        // Convertir Vector2Int en tableau d'entiers
        coord = new int[] { data.coord.x, data.coord.y };

        // Initialiser le tableau de blocs
        blocks = new int[data.blocks.GetLength(0), data.blocks.GetLength(1), data.blocks.GetLength(2)];

        // Convertir BlockType[,,] en int[,,]
        for (int x = 0; x < data.blocks.GetLength(0); x++)
        {
            for (int y = 0; y < data.blocks.GetLength(1); y++)
            {
                for (int z = 0; z < data.blocks.GetLength(2); z++)
                {
                    blocks[x, y, z] = (int)data.blocks[x, y, z];
                }
            }
        }
    }

    public ChunkData ToChunkData()
    {
        // Convertir le tableau d'entiers en Vector2Int
        Vector2Int coordVec = new Vector2Int(coord[0], coord[1]);

        // Créer une nouvelle instance de ChunkData avec les données désérialisées
        ChunkData data = new ChunkData(coordVec, blocks.GetLength(0), (x, y, z) => (BlockType)blocks[x, y, z]);
        return data;
    }
}
