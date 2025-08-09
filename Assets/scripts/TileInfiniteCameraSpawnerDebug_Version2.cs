using UnityEngine;
using UnityEngine.Tilemaps;

// This file is ONLY for debug visualization. No world gen, no invisible logic.
public class TileInfiniteCameraSpawnerDebug : MonoBehaviour
{
    [SerializeField] public Tilemap debugTilemap;         // Dedicated debug overlay tilemap!
    [SerializeField] public TileBase debugOrangeTileAsset;
    [SerializeField] public int debugRadius = 4;
    public bool debugShowHiddenTiles = false;
    public bool debugShowColliders = false; // Toggle to show colliders

    // Reference to world tilemaps for collider visualization
    [Header("World Tilemap References")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap frontTilemap;
    [SerializeField] private Tilemap backTilemap;

    void Update()
    {
        if (debugShowHiddenTiles && debugTilemap != null && debugOrangeTileAsset != null)
        {
            ShowDebugTilesHalfCircleFlipped();
        }
        else
        {
            ClearDebugTiles();
        }

        if (debugShowColliders && debugTilemap != null && debugOrangeTileAsset != null)
        {
            ShowWorldColliders();
        }
        else if (!debugShowColliders)
        {
            ClearColliderTiles();
        }
    }

    // Flipped half-circle ABOVE player (dy > 0)
    private void ShowDebugTilesHalfCircleFlipped()
    {
        if (debugTilemap == null || debugOrangeTileAsset == null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 playerWorldPos = player.transform.position;
        Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(playerWorldPos.x), Mathf.RoundToInt(playerWorldPos.y));
        int playerZ = Mathf.FloorToInt(playerWorldPos.z);

        for (int dx = -debugRadius; dx <= debugRadius; dx++)
        {
            for (int dy = 1; dy <= debugRadius; dy++)
            {
                if (dx * dx + dy * dy > debugRadius * debugRadius) continue;
                if (dx == 0 && dy == 0) continue; // Skip directly above player
                Vector2Int offset = new Vector2Int(dx, dy);
                Vector2Int tileXY = playerXY + offset;
                Vector3Int pos = new Vector3Int(tileXY.x, tileXY.y, playerZ);

                debugTilemap.SetTile(pos, debugOrangeTileAsset);
                debugTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
            }
        }
    }

    private void ClearDebugTiles()
    {
        if (debugTilemap == null || debugOrangeTileAsset == null) return;
        BoundsInt bounds = debugTilemap.cellBounds;
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
                for (int z = bounds.zMin; z <= bounds.zMax; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, z);
                    if (debugTilemap.GetTile(cellPos) == debugOrangeTileAsset)
                    {
                        debugTilemap.SetTile(cellPos, null);
                        debugTilemap.SetTransformMatrix(cellPos, Matrix4x4.identity);
                    }
                }
    }

    // Show collider tiles (visualize all world tiles with a collider in the debug radius)
    private void ShowWorldColliders()
    {
        if (debugTilemap == null || debugOrangeTileAsset == null) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 playerWorldPos = player.transform.position;
        Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(playerWorldPos.x), Mathf.RoundToInt(playerWorldPos.y));
        int playerZ = Mathf.FloorToInt(playerWorldPos.z);

        // Check all referenced tilemaps for colliders
        Tilemap[] worldTilemaps = new Tilemap[]
        {
            groundTilemap, middleFrontTilemap, middleBackTilemap, frontTilemap, backTilemap
        };

        for (int dx = -debugRadius; dx <= debugRadius; dx++)
        {
            for (int dy = -debugRadius; dy <= debugRadius; dy++)
            {
                Vector2Int offset = new Vector2Int(dx, dy);
                Vector2Int tileXY = playerXY + offset;
                for (int dz = -2; dz <= 2; dz++)
                {
                    int z = playerZ + dz;
                    Vector3Int pos = new Vector3Int(tileXY.x, tileXY.y, z);

                    foreach (var tilemap in worldTilemaps)
                    {
                        if (tilemap == null) continue;
                        TileBase tile = tilemap.GetTile(pos);
                        if (tile != null && tilemap.HasTile(pos) && tilemap.GetColliderType(pos) != Tile.ColliderType.None)
                        {
                            debugTilemap.SetTile(pos, debugOrangeTileAsset);
                            debugTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                        }
                    }
                }
            }
        }
    }

    private void ClearColliderTiles()
    {
        if (debugTilemap == null || debugOrangeTileAsset == null) return;
        BoundsInt bounds = debugTilemap.cellBounds;
        for (int x = bounds.xMin; x <= bounds.xMax; x++)
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
                for (int z = bounds.zMin; z <= bounds.zMax; z++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, z);
                    if (debugTilemap.GetTile(cellPos) == debugOrangeTileAsset)
                    {
                        debugTilemap.SetTile(cellPos, null);
                        debugTilemap.SetTransformMatrix(cellPos, Matrix4x4.identity);
                    }
                }
    }
}