using UnityEngine;

public class FlatWorld : MonoBehaviour
{
    public BlockGenerator blockGen;
    public Transform player;
    public int size = 40;

    void Start()
    {
        Vector3 playerPos = player != null ? player.position : Vector3.zero;
        int centerX = Mathf.RoundToInt(playerPos.x);
        int centerZ = Mathf.RoundToInt(playerPos.z);
        int half = size / 2;

        for (int x = -half; x < half; x++)
        {
            for (int z = -half; z < half; z++)
            {
                Vector3 pos = new Vector3(centerX + x, 0, centerZ + z);
                blockGen.CreateBlock(pos, "grass_block_top.png", "dirt.png", "grass_block_side.png");
            }
        }

        // Place le joueur juste au-dessus du centre du terrain
        if (player != null)
        {
            player.position = new Vector3(centerX, 3f, centerZ); // y=3 pour être sûr d'être au-dessus du sol
        }
    }
}
