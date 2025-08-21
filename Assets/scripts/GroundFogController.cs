using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[System.Serializable]
public class FogLayerSettings
{
    public string name = "Fog Layer";
    [Header("Target (for overlay reference only)")]
    public Tilemap targetTilemap;
    [Header("Fog Output")]
    public Tilemap fogTilemap;
    public TileBase fogTile;
    [Header("Layering and Appearance")]
    public Color fogColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

    [Header("Fog Logic")]
    public bool hideIfNextToAir = true; // Toggle: should this layer check for adjacent air
    [Tooltip("The Z offset relative to the player's current Z. 0 = player Z, 1 = in front, -1 = behind, etc.")]
    public int fogZOffset = 0; // Offset relative to player Z
}

public class GroundFogController : MonoBehaviour
{
    [Header("Fog Layers - Each with its own tilemap, color, and tile")]
    public FogLayerSettings[] fogLayers;

    [Header("Player Reference")]
    public Transform player;

    [Header("Procedural World Reference")]
    public TileInfiniteCameraSpawner worldSpawner;

    [Header("Tile Hidden Set Reference")]
    public TileHiddenSet tileHiddenSet; // Reference to your TileHiddenSet script

    [Header("Camera Reference")]
    public Camera mainCamera;

    [Header("Camera Buffer (tiles)")]
    public int cameraBuffer = 5;

    // Cache for neighbor offsets (8 directions)
    private static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
    {
        new Vector3Int(-1, -1, 0), new Vector3Int(0, -1, 0), new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 0, 0),                      new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 1, 0),  new Vector3Int(0, 1, 0),  new Vector3Int(1, 1, 0)
    };

    void Start()
    {
        // Ensure all fog tilemaps have colliders off for visual effect
        foreach (var layer in fogLayers)
        {
            if (layer.fogTilemap == null) continue;
            var collider = layer.fogTilemap.GetComponent<TilemapCollider2D>();
            if (collider != null)
                collider.enabled = false;
        }
    }

    void Update()
    {
        if (player == null || worldSpawner == null || fogLayers == null || tileHiddenSet == null || mainCamera == null)
            return;

        int playerZ = Mathf.RoundToInt(player.position.z);

        // Get camera bounds in world coordinates
        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));

        // Convert to tile coordinates, expanding by buffer
        int minX = Mathf.FloorToInt(camMin.x) - cameraBuffer;
        int maxX = Mathf.CeilToInt(camMax.x) + cameraBuffer;
        int minY = Mathf.FloorToInt(camMin.y) - cameraBuffer;
        int maxY = Mathf.CeilToInt(camMax.y) + cameraBuffer;

        // Use references only once
        HashSet<Vector3Int> activeTiles = worldSpawner.GetActiveTiles();
        HashSet<Vector3Int> toHide = tileHiddenSet.GetTilesToHide(player.position);

        foreach (var layer in fogLayers)
        {
            if (layer.fogTilemap == null || layer.fogTile == null)
                continue;

            layer.fogTilemap.ClearAllTiles();
            layer.fogTilemap.color = layer.fogColor;

            int fogLayerZ = playerZ + layer.fogZOffset;

            foreach (var tile in activeTiles)
            {
                if (tile.z != fogLayerZ ||
                    tile.x < minX || tile.x > maxX ||
                    tile.y < minY || tile.y > maxY)
                    continue;

                // Skip if this tile should be hidden (from the hide bubble)
                if (toHide.Contains(tile))
                    continue;

                // Per-layer: Only hide if next to air if the option is enabled, ON THIS LAYER'S Z
                if (layer.hideIfNextToAir)
                {
                    bool adjacentToAir = false;
                    foreach (var offset in neighborOffsets)
                    {
                        Vector3Int neighborPos = new Vector3Int(tile.x + offset.x, tile.y + offset.y, fogLayerZ); // only this z!
                        TileType neighborType = worldSpawner.GetTileTypeForFog(neighborPos, TileType.Dirt);
                        if (neighborType == TileType.Air)
                        {
                            adjacentToAir = true;
                            break;
                        }
                    }
                    if (adjacentToAir)
                        continue;
                }

                layer.fogTilemap.SetTile(tile, layer.fogTile);
                layer.fogTilemap.SetColliderType(tile, Tile.ColliderType.None);
            }
        }
    }
}