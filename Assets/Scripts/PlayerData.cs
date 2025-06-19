using UnityEngine;

[System.Serializable]
public class PlayerData
{
    public float x, y, z;
    public string[] inventory; // Simplifié

    public PlayerData(Vector3 pos, string[] inv)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
        inventory = inv;
    }

    public Vector3 GetPosition() => new Vector3(x, y, z);
}
