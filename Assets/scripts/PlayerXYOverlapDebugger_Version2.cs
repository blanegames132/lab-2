using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerTileBehindDebugger : MonoBehaviour
{
    public Tilemap backTilemap;
    public Tilemap middleBackTilemap;
    public Transform player;
    public bool isBlocked;

    [Tooltip("How many tiles to search along the Z axis (positive and negative)")]
    public int zSearchRange = 1;

    void Awake()
    {
        // Try to auto-find the player by tag if not set in the inspector
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3Int playerCell = Vector3Int.FloorToInt(player.position);

        // Find first tile in line with player (X, Y), searching along Z+
        if (middleBackTilemap != null)
        {
            TileBase foundTile = null;
            Vector3Int foundPos = Vector3Int.zero;
            bool isBlocked = false; // Default to false

            for (int dz = 1; dz <= zSearchRange; dz++)
            {
                Vector3Int checkPos = new Vector3Int(playerCell.x, playerCell.y, playerCell.z + dz);
                TileBase tile = middleBackTilemap.GetTile(checkPos);
                if (tile != null)
                {
                    foundTile = tile;
                    foundPos = checkPos;
                    isBlocked = true;
                    break;
                }
            }

            if (foundTile != null)
            {
                Debug.Log($"First tile in line with player in middleBackTilemap at {foundPos}: {foundTile.name}");
                Debug.Log("Player position: " + player.position);
            }
            else
            {
                Debug.Log("No tile found in middleBackTilemap within search range.");
            }

            // Log isBlocked regardless of the result
            Debug.Log("isBlocked: " + isBlocked);
        }

        if (backTilemap != null)
        {
            TileBase foundTile = null;
            Vector3Int foundPos = Vector3Int.zero;
            for (int dz = 1; dz <= zSearchRange; dz++)
            {
                Vector3Int checkPos = new Vector3Int(playerCell.x, playerCell.y, playerCell.z + dz);
                TileBase tile = backTilemap.GetTile(checkPos);
                if (tile != null)
                {
                    foundTile = tile;
                    foundPos = checkPos;
                    break;
                }
            }
            if (foundTile != null)
            {
                Debug.Log($"First tile in line with player in backTilemap at {foundPos}: {foundTile.name}");
            }
        }
    }
}