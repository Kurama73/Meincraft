using System;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    public Vector2Int coord;
    public BlockType[,,] blocks;

    public ChunkData(Vector2Int coord, int size)
    {
        this.coord = coord;
        blocks = new BlockType[size, 64, size];
    }

    public BlockType GetBlock(int x, int y, int z)
    {
        if (x < 0 || y < 0 || z < 0 || x >= blocks.GetLength(0) || y >= blocks.GetLength(1) || z >= blocks.GetLength(2))
            return BlockType.air;
        return blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || y < 0 || z < 0 || x >= blocks.GetLength(0) || y >= blocks.GetLength(1) || z >= blocks.GetLength(2))
            return;
        blocks[x, y, z] = type;
    }
}
