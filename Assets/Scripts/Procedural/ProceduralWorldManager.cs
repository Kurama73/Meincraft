using UnityEngine;
using System.Collections;

public class ProceduralWorldManager : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSeed = 12345;
    public float noiseScale = 0.1f;
    public int maxHeight = 10;

    [Header("Performance")]
    public int chunksPerFrame = 1;
    public float updateInterval = 0.1f;

    [Header("References")]
    public ChunkManager chunkManager;
    public Transform player;

    void Start()
    {
        // Initialiser le seed pour la génération procédurale
        Random.InitState(worldSeed);

        // Démarrer la routine d'optimisation
        StartCoroutine(OptimizationLoop());

        // Assurez-vous que les chunks initiaux sont générés avant de positionner le joueur
        StartCoroutine(GenerateInitialChunksAndSpawnPlayer());
    }

    IEnumerator GenerateInitialChunksAndSpawnPlayer()
    {
        yield return new WaitForSeconds(1f); // Attendre que les chunks initiaux soient générés

        // Positionner le joueur
        player.position = GetSpawnPosition();
    }


    IEnumerator OptimizationLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Nettoyer la mémoire si nécessaire
            if (Time.frameCount % 300 == 0) // Toutes les 5 secondes à 60 FPS
            {
                System.GC.Collect();
            }
        }
    }

    public float GetHeightAtPosition(float x, float z)
    {
        float noiseValue = Mathf.PerlinNoise(x * noiseScale, z * noiseScale);
        return noiseValue * maxHeight;
    }

    public Vector3 GetSpawnPosition()
    {
        Vector3 spawnPos = Vector3.zero;
        spawnPos.y = GetHeightAtPosition(0, 0) + 3f;

        // Assurez-vous que la hauteur est valide
        if (spawnPos.y <= 0)
        {
            spawnPos.y = 3f; // Hauteur de secours si la hauteur du terrain est invalide
        }

        return spawnPos;
    }

}