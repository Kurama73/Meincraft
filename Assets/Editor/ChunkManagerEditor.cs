#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkManager))]
public class ChunkManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ChunkManager manager = (ChunkManager)target;
        if (GUILayout.Button("Générer les chunks autour du joueur"))
        {
            manager.GenerateInitialChunks();
        }
        if (GUILayout.Button("Supprimer tous les chunks"))
        {
            manager.ClearAllChunks();
        }
    }
}
#endif
