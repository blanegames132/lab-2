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

    [Tooltip("Reference to InfiniteCameraSpawnerModular spawner.")]
    public InfiniteCameraSpawnerModular spawner; // <-- Assign in Inspector or via code

    [Tooltip("Second visual radius for determinant/hide area (in tiles, XY). Set to 0 to disable.")]
    public int determinantRadius = 0;

    [Tooltip("Vertical offset for gizmo visualization.")]
    public float gizmoVerticalOffset = 0.25f;

    [Tooltip("If true, will set the hide tile at all hidden positions every frame.")]
    public bool setHideTilesRuntime = true;

    /// <summary>
    /// Dynamically sets bubbleHideRadius for configs based on active tilemap index.
    /// E.g. if activeIndex==2, sets configs[2]=2, configs[3]=4, configs[4]=6, etc.
    /// </summary>
    public void SetBubbleHideRadiusByActive(int activeIndex)
    {
        if (configs == null) return;
        int radius = 2;
        for (int i = activeIndex; i < configs.Count; i++, radius += 2)
        {
            configs[i].bubbleHideRadius = radius;
        }
    }

    /// <summary>
    /// Hide a single tile if it falls within the bubble radius for any configured tilemap.
    /// Call this from your spawner after setting a tile.
    /// </summary>
    public void HideTileIfShould(Tilemap tilemap, Vector3Int cell)
    {
        foreach (var config in configs)
        {
            if (config == null || config.tilemap != tilemap || triggerTransform == null) continue;

            Vector3Int centerCell = config.tilemap.WorldToCell(triggerTransform.position);
            int dx = cell.x - centerCell.x;
            int dy = cell.y - centerCell.y;

            // Only hide if within bubble radius
            if (dx * dx + dy * dy <= config.bubbleHideRadius * config.bubbleHideRadius)
            {
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
                // Archive/cave support
                TileEventBus.BroadcastTileSet(config.tilemap, cell, null);
            }
        }
    }

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

        foreach (var config in configs)
        {
            if (config == null || config.tilemap == null)
                continue;

            HashSet<Vector3Int> hidden = GetTilesToHide(config, triggerTransform.position);
            foreach (var cell in hidden)
            {
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
                if (TileEventBus.QueryShouldBlockTile(cell)) continue; // Already blocked
                TileEventBus.BroadcastTileSet(config.tilemap, cell, null); // null means "air" for archiving
            }
        }
    }

#if UNITY_EDITOR
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