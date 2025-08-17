using System.Collections.Generic;
using UnityEngine;

public class TileHiddenSet : MonoBehaviour
{
    [Tooltip("Bubble radius for hiding tiles under the player.")]
    public int bubbleHideRadius = 4;

    // Returns the set of positions that should be hidden for the current player position
    public HashSet<Vector3Int> GetTilesToHide(Vector3 playerWorldPos)
    {
        Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(playerWorldPos.x), Mathf.RoundToInt(playerWorldPos.y));
        int playerZ = Mathf.FloorToInt(playerWorldPos.z);

        int[] layerZs = new int[] { playerZ - 2, playerZ - 1, playerZ, playerZ + 1, playerZ + 2 };

        HashSet<Vector3Int> toHide = new HashSet<Vector3Int>();

        for (int dx = -bubbleHideRadius; dx <= bubbleHideRadius; dx++)
        {
            for (int dy = -bubbleHideRadius; dy <= 0; dy++)
            {
                if (dx * dx + dy * dy > bubbleHideRadius * bubbleHideRadius) continue;
                Vector2Int offset = new Vector2Int(dx, dy);
                Vector2Int tileXY = playerXY + offset;
                foreach (int z in layerZs)
                {
                    Vector3Int pos = new Vector3Int(tileXY.x, tileXY.y, z);
                    if (tileXY == playerXY && z == playerZ) continue;
                    toHide.Add(pos);
                }
            }
        }
        return toHide;
    }
}