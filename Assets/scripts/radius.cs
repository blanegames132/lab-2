using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Deletes all fog tiles in a radius (absolutely) from the specified tilemaps,
/// and marks all "cave" tiles as discovered in the main ChunkedWorldArchive.
/// IMPORTANT: permanence of fog tiles (never respawn) is handled by TileCaveUtility.
/// </summary>
public class DeleteAllFogInRadius : MonoBehaviour
{
    public Tilemap[] targetTilemaps;        // All fog tilemaps to clear
    public ChunkedWorldArchive archive;     // Main archive for cave type check and discovery marking
    public Transform centerTransform;       // Usually your player
    public float radius = 4f;               // Radius in WORLD units!
    public bool runEveryFrame = true;
    public bool debug = true;

    void Update()
    {
        if (runEveryFrame)
            DeleteAllFogIfPlayerInRadius();
    }

    public void DeleteAllFogIfPlayerInRadius()
    {
        if (targetTilemaps == null || targetTilemaps.Length == 0 || centerTransform == null)
        {
            if (debug) Debug.LogWarning("DeleteAllFogInRadius: Missing references!");
            return;
        }

        Vector3 center = centerTransform.position;
        bool anyDiscovered = false;

        foreach (var tilemap in targetTilemaps)
        {
            if (tilemap == null) continue;

            var toDelete = new System.Collections.Generic.List<Vector3Int>();

            foreach (var cell in tilemap.cellBounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(cell)) continue;

                Vector3 tileWorld = tilemap.GetCellCenterWorld(cell);
                if (Vector3.Distance(tileWorld, center) <= radius)
                {
                    // If it's a cave in the archive, mark as discovered
                    if (archive != null)
                    {
                        TileData data = archive.TryGetTile(cell);
                        if (data != null && data.blockTagOrName == "cave" && !data.discovered)
                        {
                            data.discovered = true;
                            archive.SetTile(cell, data);
                            anyDiscovered = true;
                            if (debug) Debug.Log($"Marked cave at {cell} as discovered in archive");
                        }
                    }

                    toDelete.Add(cell);
                }
            }

            foreach (var cell in toDelete)
            {
                tilemap.SetTile(cell, null);
                if (debug) Debug.Log($"Deleted fog tile at {cell} ({tilemap.name})");
            }
        }

        if (anyDiscovered && archive != null)
            archive.SaveAll();
    }
}