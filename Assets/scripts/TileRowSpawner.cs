using UnityEngine;
using UnityEngine.Tilemaps;

public class TileRowSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase groundTileAsset;   // dirt
    [SerializeField] private TileBase bushTileAsset;
    [SerializeField] private TileBase grassTileAsset;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int tileSpawnDistance = 100;    // Distance ahead/behind the player (x) to spawn tiles
    [SerializeField] private int tileDeleteDistance = 200;   // Distance ahead/behind the player (x) to delete tiles
    [SerializeField] private int zLimit = 500;               // Max z index (exclusive)

    private int leftSpawnedX;
    private int rightSpawnedX;
    private const int dirtYOffset = -100;

    private void Start()
    {
        int playerX = Mathf.FloorToInt(playerTransform.position.x);
        leftSpawnedX = playerX;
        rightSpawnedX = playerX;

        // Spawn row at player X for all z
        for (int z = 0; z < zLimit; z++)
        {
            groundTilemap.SetTile(new Vector3Int(playerX, 0, z), grassTileAsset);
            groundTilemap.SetTile(new Vector3Int(playerX, dirtYOffset, z), groundTileAsset);
        }
    }

    private void Update()
    {
        int playerX = Mathf.FloorToInt(playerTransform.position.x);

        // Spawn tiles to the right of the player
        while (rightSpawnedX < playerX + tileSpawnDistance)
        {
            rightSpawnedX++;
            for (int z = 0; z < zLimit; z++)
            {
                groundTilemap.SetTile(new Vector3Int(rightSpawnedX, 0, z), grassTileAsset);
                groundTilemap.SetTile(new Vector3Int(rightSpawnedX, dirtYOffset, z), groundTileAsset);
            }
        }

        // Spawn tiles to the left of the player
        while (leftSpawnedX > playerX - tileSpawnDistance)
        {
            leftSpawnedX--;
            for (int z = 0; z < zLimit; z++)
            {
                groundTilemap.SetTile(new Vector3Int(leftSpawnedX, 0, z), grassTileAsset);
                groundTilemap.SetTile(new Vector3Int(leftSpawnedX, dirtYOffset, z), groundTileAsset);
            }
        }

        // Delete tiles too far right
        int maxRightDelete = playerX + tileDeleteDistance;
        for (int x = rightSpawnedX; x > maxRightDelete; x--)
        {
            for (int z = 0; z < zLimit; z++)
            {
                groundTilemap.SetTile(new Vector3Int(x, 0, z), null);
                groundTilemap.SetTile(new Vector3Int(x, dirtYOffset, z), null);
            }
        }

        // Delete tiles too far left
        int maxLeftDelete = playerX - tileDeleteDistance;
        for (int x = leftSpawnedX; x < maxLeftDelete; x++)
        {
            for (int z = 0; z < zLimit; z++)
            {
                groundTilemap.SetTile(new Vector3Int(x, 0, z), null);
                groundTilemap.SetTile(new Vector3Int(x, dirtYOffset, z), null);
            }
        }
    }
}