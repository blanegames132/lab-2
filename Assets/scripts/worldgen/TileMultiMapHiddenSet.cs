using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TilemapHideConfig
{
    public Tilemap tilemap;
    [Tooltip("Bubble radius (in tiles, XY) for hiding on this tilemap.")]
    public int bubbleHideRadius = 4;
    [Tooltip("Hide tile to set at hidden positions in this tilemap (leave empty to remove/clear tiles instead of hiding).")]
    public TileBase hideTileAsset;
}

[ExecuteAlways]
[DisallowMultipleComponent]
public class TileMultiMapHiddenSet : MonoBehaviour
{
    [Tooltip("Per-tilemap configs: assign tilemap, radius and hide tile here.")]
    public List<TilemapHideConfig> configs = new List<TilemapHideConfig>();

    [Tooltip("Transform (e.g. Player) used as the center of the hide area.")]
    public Transform triggerTransform;

    [Tooltip("Reference to ChunkedWorldArchive or spawner with archive.")]
    public InfiniteCameraSpawnerModular spawner; // <-- Assign in Inspector or via code

    [Tooltip("Second visual radius for determinant/hide area (in tiles, XY). Set to 0 to disable.")]
    public int determinantRadius = 0;

    [Tooltip("Vertical offset for gizmo visualization.")]
    public float gizmoVerticalOffset = 0.25f;

    [Tooltip("If true, will set the hide tile at all hidden positions every frame.")]
    public bool setHideTilesRuntime = true;

    /// <summary>
    /// Returns the set of hidden tile positions for a given tilemap, at *all* z's, within the bubble radius from trigger.
    /// </summary>
    public HashSet<Vector3Int> GetTilesToHide(TilemapHideConfig config, Vector3 triggerWorldPos)
    {
        var toHide = new HashSet<Vector3Int>();
        if (config == null || config.tilemap == null) return toHide;

        Vector3Int centerCell = config.tilemap.WorldToCell(triggerWorldPos);
        BoundsInt bounds = config.tilemap.cellBounds;

        for (int dx = -config.bubbleHideRadius; dx <= config.bubbleHideRadius; dx++)
        {
            for (int dy = -config.bubbleHideRadius; dy <= config.bubbleHideRadius; dy++)
            {
                if (dx * dx + dy * dy > config.bubbleHideRadius * config.bubbleHideRadius) continue;
                int x = centerCell.x + dx;
                int y = centerCell.y + dy;

                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);
                    if (config.tilemap.GetTile(cell) != null)
                        toHide.Add(cell);
                }
            }
        }
        return toHide;
    }

    void Update()
    {
        if (!setHideTilesRuntime || configs == null || triggerTransform == null)
            return;

        // Try to find archive
        ChunkedWorldArchive archive = null;
        // FIX: use spawner.worldArchiveManager.worldArchive, not spawner.worldArchive directly
        if (spawner != null && spawner.enableWorldArchive && spawner.worldArchiveManager != null && spawner.worldArchiveManager.worldArchive != null)
            archive = spawner.worldArchiveManager.worldArchive;

        bool archiveChanged = false;

        foreach (var config in configs)
        {
            if (config == null || config.tilemap == null)
                continue;

            HashSet<Vector3Int> hidden = GetTilesToHide(config, triggerTransform.position);
            foreach (var cell in hidden)
            {
                // Defensive: If hideTileAsset is set, use it; if not, REMOVE the tile instead (delete/clear).
                if (config.hideTileAsset != null)
                {
                    if (config.tilemap.GetTile(cell) != config.hideTileAsset)
                        config.tilemap.SetTile(cell, config.hideTileAsset);
                }
                else
                {
                    if (config.tilemap.GetTile(cell) != null)
                        config.tilemap.SetTile(cell, null);
                }

                // --- ARCHIVE SUPPORT: if this was a cave, set to air and NEVER reset to cave again ---
                if (archive != null)
                {
                    TileData data = archive.TryGetTile(cell);
                    if (data != null && data.blockTagOrName == "cave")
                    {
                        // Only replace if it is still a cave (never revert to cave after air)
                        archive.SetTile(cell, new TileData { blockTagOrName = "air", discovered = false });
                        archiveChanged = true;
                    }
                }
            }
        }

        if (archiveChanged && archive != null)
            archive.SaveAll();
    }

#if UNITY_EDITOR
    // Always active—never toggled or gated by selection
    void OnDrawGizmos()
    {
        DrawHiddenTilesGizmos();
    }
    void OnDrawGizmosSelected()
    {
        DrawHiddenTilesGizmos();
    }
    public static HashSet<Vector3Int> GetTilesToHideStatic(TilemapHideConfig config, Vector3 triggerWorldPos)
    {
        var toHide = new HashSet<Vector3Int>();
        if (config == null || config.tilemap == null) return toHide;

        Vector3Int centerCell = config.tilemap.WorldToCell(triggerWorldPos);
        BoundsInt bounds = config.tilemap.cellBounds;

        for (int dx = -config.bubbleHideRadius; dx <= config.bubbleHideRadius; dx++)
        {
            for (int dy = -config.bubbleHideRadius; dy <= config.bubbleHideRadius; dy++)
            {
                if (dx * dx + dy * dy > config.bubbleHideRadius * config.bubbleHideRadius) continue;
                int x = centerCell.x + dx;
                int y = centerCell.y + dy;

                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);
                    if (config.tilemap.GetTile(cell) != null)
                        toHide.Add(cell);
                }
            }
        }
        return toHide;
    }
    private void DrawHiddenTilesGizmos()
    {
        if (triggerTransform == null || configs == null) return;
        foreach (var config in configs)
        {
            if (config == null || config.tilemap == null) continue;
            // Draw the determinant (outer) radius as a wire circle on the tilemap grid (at trigger Z)
            if (determinantRadius > 0)
            {
                Vector3Int centerCell = config.tilemap.WorldToCell(triggerTransform.position);
                int centerZ = centerCell.z;

                Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
                for (int dx = -determinantRadius; dx <= determinantRadius; dx++)
                {
                    for (int dy = -determinantRadius; dy <= determinantRadius; dy++)
                    {
                        if (dx * dx + dy * dy > determinantRadius * determinantRadius) continue;
                        if (dx * dx + dy * dy < (determinantRadius - 1) * (determinantRadius - 1)) continue;
                        Vector3Int cell = new Vector3Int(centerCell.x + dx, centerCell.y + dy, centerZ);
                        Vector3 cellCenter = config.tilemap.GetCellCenterWorld(cell);
                        Gizmos.DrawWireCube(cellCenter + Vector3.up * gizmoVerticalOffset, config.tilemap.cellSize);
                    }
                }
            }

            // Draw hidden tile cubes (on ALL z's)
            var hidden = GetTilesToHide(config, triggerTransform.position);
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.38f);
            foreach (var cell in hidden)
            {
                Vector3 cellCenter = config.tilemap.GetCellCenterWorld(cell);
                Gizmos.DrawCube(cellCenter + Vector3.up * gizmoVerticalOffset, config.tilemap.cellSize * 0.92f);
            }
        }
    }
#endif
}