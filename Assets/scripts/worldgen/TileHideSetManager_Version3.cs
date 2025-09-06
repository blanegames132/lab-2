using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Absolutely hides (or deletes) all tiles in the specified area every frame, even if no hide asset is specified.
/// Hides (or deletes) ALL tiles in the radius at every Z in the tilemap bounds, not just the player's Z.
/// Logs a debug message if there is no tile to hide.
/// When outside the radius, no replacement or restore is attempted.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class TileHideSetManager : MonoBehaviour
{
    [Header("Player Reference")]
    public Transform playerTransform;

    [Header("Tilemap Hide Configs")]
    public List<TilemapHideConfig> hideConfigs = new List<TilemapHideConfig>();

    void Update()
    {
        if (playerTransform == null)
            return;

        foreach (var config in hideConfigs)
        {
            if (config == null || config.tilemap == null)
                continue;

            Vector3Int centerCell = config.tilemap.WorldToCell(playerTransform.position);
            int radius = config.bubbleHideRadius;
            BoundsInt bounds = config.tilemap.cellBounds;

            // Iterate through all Zs in the tilemap's bounds!
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int x = centerCell.x + dx;
                    int y = centerCell.y + dy;

                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        Vector3Int cell = new Vector3Int(x, y, z);
                        TileBase currentTile = config.tilemap.GetTile(cell);
                        if (currentTile != null)
                        {
                            // Overwrite: if hideTileAsset is set, use it; otherwise set null
                            config.tilemap.SetTile(cell, config.hideTileAsset ?? null);
                        }
                        else
                        {
                            Debug.Log($"[TileHideSetManager] Cannot hide cell {cell} in tilemap {config.tilemap.name}: no tile present to hide or delete.");
                        }
                    }
                }
        }
    }
}