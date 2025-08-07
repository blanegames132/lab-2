using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileRowSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase groundTileAsset;   // dirt
    [SerializeField] private TileBase grassTileAsset;    // grass
    [SerializeField] private Transform playerTransform;

    private int grassBand = 20;
    private int dirtDepth = 10;
    private int dirtAllowedDistance = 50;
    private int zLayers = 2; // Number of layers in front/behind player

    private HashSet<Vector3Int> spawnedTiles = new HashSet<Vector3Int>();
    private int lastPlayerX = int.MinValue;
    private int lastPlayerZ = int.MinValue;

    void Update()
    {
        int playerX = Mathf.FloorToInt(playerTransform.position.x);
        int playerZ = Mathf.FloorToInt(playerTransform.position.z);

        // Only spawn when the player moves to a new column or layer
        if (playerX != lastPlayerX || playerZ != lastPlayerZ)
        {
            for (int x = playerX - grassBand; x <= playerX + grassBand; x++)
            {
                for (int z = playerZ - zLayers; z <= playerZ + zLayers; z++)
                {
                    // Spawn grass row
                    Vector3Int grassPos = new Vector3Int(x, 0, z);
                    if (!spawnedTiles.Contains(grassPos))
                    {
                        groundTilemap.SetTile(grassPos, grassTileAsset);
                        spawnedTiles.Add(grassPos);
                    }

                    // Only spawn dirt if within 50 of the player
                    if (Mathf.Abs(x - playerX) <= dirtAllowedDistance)
                    {
                        for (int y = -1; y >= -dirtDepth; y--)
                        {
                            Vector3Int dirtPos = new Vector3Int(x, y, z);
                            if (!spawnedTiles.Contains(dirtPos))
                            {
                                groundTilemap.SetTile(dirtPos, groundTileAsset);
                                spawnedTiles.Add(dirtPos);
                            }
                        }
                    }
                }
            }
            lastPlayerX = playerX;
            lastPlayerZ = playerZ;
        }
    }
}