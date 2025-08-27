using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Draws your TileBase asset on EVERY position covering all 6 sides of a 100x100x100 cube
/// centered on the player, in all X, Y, Z directions. All faces/sides, every frame.
/// No prefab required, just the TileBase asset.
/// </summary>
public class DrawCubeAllDirectionsTilemap : MonoBehaviour
{
    [SerializeField] private Tilemap targetTilemap;      // Assign in Inspector
    [SerializeField] private TileBase assetTile;         // Assign in Inspector (TileBase asset)
    [SerializeField] private Transform playerTransform;  // Assign in Inspector

    private const int CubeSize = 100;
    private const int CubeHalf = CubeSize / 2;

    void Update()
    {
        if (targetTilemap == null || assetTile == null || playerTransform == null)
            return;

        Vector3Int center = Vector3Int.RoundToInt(playerTransform.position);

        int minX = center.x - CubeHalf;
        int maxX = center.x + CubeHalf - 1;
        int minY = center.y - CubeHalf;
        int maxY = center.y + CubeHalf - 1;
        int minZ = center.z - CubeHalf;
        int maxZ = center.z + CubeHalf - 1;

        // Draw X faces (minX, maxX)
        for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                targetTilemap.SetTile(new Vector3Int(minX, y, z), assetTile);
                targetTilemap.SetTile(new Vector3Int(maxX, y, z), assetTile);
            }

        // Draw Y faces (minY, maxY)
        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                targetTilemap.SetTile(new Vector3Int(x, minY, z), assetTile);
                targetTilemap.SetTile(new Vector3Int(x, maxY, z), assetTile);
            }

        // Draw Z faces (minZ, maxZ)
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                targetTilemap.SetTile(new Vector3Int(x, y, minZ), assetTile);
                targetTilemap.SetTile(new Vector3Int(x, y, maxZ), assetTile);
            }
    }
}