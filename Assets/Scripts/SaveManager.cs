using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public Transform player;
    public string currentWorld;

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
    }
}
