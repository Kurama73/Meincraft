using UnityEngine;

public enum BiomeType
{
    Plains,
    Forest,
    Mountain,
    Desert,
    Ocean,
    Taiga,
    Swamp,
    Beach
}

[System.Serializable]
public class Biome
{
    public BiomeType type;
    public float baseHeight;
    public float heightVariation;
    public Color color;
    public float temperature;
    public float humidity;

    public Biome(BiomeType type, float baseHeight, float heightVariation, Color color)
    {
        this.type = type;
        this.baseHeight = baseHeight;
        this.heightVariation = heightVariation;
        this.color = color;
        this.temperature = 0.5f;
        this.humidity = 0.5f;
    }

    public Biome(BiomeType type, float baseHeight, float heightVariation, Color color, float temperature, float humidity)
    {
        this.type = type;
        this.baseHeight = baseHeight;
        this.heightVariation = heightVariation;
        this.color = color;
        this.temperature = temperature;
        this.humidity = humidity;
    }
}
