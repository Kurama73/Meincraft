using UnityEngine;

public enum GenerationStage
{
    NotGenerated = 0,
    Terrain = 1,
    Caves = 2,
    Decorations = 3,
    Trees = 4,
    Rivers = 5,
    Structures = 6,
    Resources = 7,
    Lighting = 8,
    Complete = 9
}

[System.Serializable]
public class ChunkGenerationState
{
    public Vector2Int coord;
    public GenerationStage currentStage;
    public bool isBeingGenerated;
    public float generationProgress;

    public ChunkGenerationState(Vector2Int coord)
    {
        this.coord = coord;
        this.currentStage = GenerationStage.NotGenerated;
        this.isBeingGenerated = false;
        this.generationProgress = 0f;
    }

    public bool IsStageComplete(GenerationStage stage)
    {
        return currentStage >= stage;
    }
}
