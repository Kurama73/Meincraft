using UnityEngine;
using System.Collections.Generic;
using System;

[System.Serializable]
public class SerializableChunkData
{
    public int[] coord; // [x, y]
    public int size;
    public List<BlockData> blocks; // Liste des blocs avec leurs positions et types

    public SerializableChunkData(ChunkData data)
    {
        coord = new int[] { data.coord.x, data.coord.y };
        size = data.blocks.GetLength(0);
        blocks = new List<BlockData>();

        for (int x = 0; x < size; x++)
            for (int y = 0; y < 64; y++)
                for (int z = 0; z < size; z++)
                {
                    BlockType block = data.blocks[x, y, z];
                    if (block != BlockType.air)
                    {
                        blocks.Add(new BlockData(x, y, z, (int)block));
                    }
                }
    }


    public ChunkData ToChunkData()
    {
        Vector2Int c = new Vector2Int(coord[0], coord[1]);
        ChunkData chunkData = new ChunkData(c, size);

        foreach (BlockData blockData in blocks)
        {
            chunkData.SetBlock(blockData.x, blockData.y, blockData.z, (BlockType)blockData.type);
        }

        return chunkData;
    }
}

[Serializable]
public class BlockData
{
    public int x;
    public int y;
    public int z;
    public int type;

    public BlockData(int x, int y, int z, int type)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.type = type;
    }
}