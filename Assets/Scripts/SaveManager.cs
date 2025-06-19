using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public Transform player;
    public string currentWorld;

    // Structure pour stocker les données du monde
    [System.Serializable]
    public class WorldData
    {
        public int seed;
    }

    // Structure pour stocker les données du joueur
    [System.Serializable]
    public class PlayerPositionData
    {
        public float x;
        public float y;
        public float z;
    }

    public void SaveWorldSeed(int seed)
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "world.json");
        WorldData data = new WorldData { seed = seed };
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    public int LoadWorldSeed()
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "world.json");
        if (File.Exists(path))
        {
            WorldData data = JsonUtility.FromJson<WorldData>(File.ReadAllText(path));
            return data.seed;
        }
        return -1; // Seed non trouvée
    }

    public void SavePlayerPosition()
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "player_position.json");
        PlayerPositionData data = new PlayerPositionData { x = player.position.x, y = player.position.y, z = player.position.z };
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    public Vector3 LoadPlayerPosition()
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "player_position.json");
        if (File.Exists(path))
        {
            PlayerPositionData data = JsonUtility.FromJson<PlayerPositionData>(File.ReadAllText(path));
            return new Vector3(data.x, data.y, data.z);
        }
        return Vector3.zero; // Position par défaut si le fichier n'existe pas
    }

    public void SavePlayer()
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "player.json");
        PlayerData data = new PlayerData(player.position, new string[] { "wood", "stone" });
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }

    public void LoadPlayer()
    {
        string path = Path.Combine(SaveSystem.GetWorldPath(currentWorld), "player.json");
        if (File.Exists(path))
        {
            PlayerData data = JsonUtility.FromJson<PlayerData>(File.ReadAllText(path));
            player.position = data.GetPosition();
        }
    }

    void OnApplicationQuit()
    {
        SavePlayer(); // Sauvegarde automatique quand tu quittes la scène ou le jeu
        SavePlayerPosition(); // Sauvegarde de la position du joueur
    }
}
