using System.Collections.Generic;
using UnityEngine;

public class RiverGenerator : MonoBehaviour
{
    [Header("River Settings")]
    public float riverFrequency = 0.02f;
    public int riverLength = 32;
    public float riverNoiseScale = 0.01f;
    public float riverCurveIntensity = 1.0f;

    public bool ShouldGenerateRiver(Vector2Int chunkCoord)
    {
        float noise = Mathf.PerlinNoise(chunkCoord.x * riverFrequency, chunkCoord.y * riverFrequency);
        return noise > 0.8f;
    }

    public List<Vector2> GenerateRiverPath(Vector2Int startChunk)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 currentPos = new Vector2(startChunk.x * 16 + 8, startChunk.y * 16 + 8);
        Vector2 direction = Random.insideUnitCircle.normalized;

        for (int i = 0; i < riverLength; i++)
        {
            path.Add(currentPos);

            // Ajouter de la courbe à la rivière
            float noise = Mathf.PerlinNoise(currentPos.x * riverNoiseScale, currentPos.y * riverNoiseScale);
            float angle = noise * riverCurveIntensity * Mathf.PI;

            direction = new Vector2(
                direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle),
                direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle)
            ).normalized;

            currentPos += direction * 2f;
        }

        return path;
    }
}