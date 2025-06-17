using UnityEngine;

public enum BiomeType
{
    Plains,
    Forest,
    Mountain,
    Desert,
    Ocean
}

public class Biome
{
    public BiomeType type;
    public float baseHeight;
    public float heightVariation;
    public Color color;

    public Biome(BiomeType type, float baseHeight, float heightVariation, Color color)
    {
        this.type = type;
        this.baseHeight = baseHeight;
        this.heightVariation = heightVariation;
        this.color = color;
    }
}
