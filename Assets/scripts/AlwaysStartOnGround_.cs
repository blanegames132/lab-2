using UnityEngine;
using UnityEngine.Tilemaps;

public class AlwaysStartOnGround : MonoBehaviour
{
    [SerializeField] private TileInfiniteCameraSpawner spawner;
    [SerializeField] private Tilemap groundTilemap; // Assign your ground tilemap here

    void Start()
    {
        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        if (!groundTilemap) groundTilemap = FindObjectOfType<Tilemap>();
        ClampToNearestGroundAtStart();

        // Reset all caves to undiscovered at game start
        ResetAllCavesToUndiscovered();
    }

    void ClampToNearestGroundAtStart()
    {
        if (spawner == null || groundTilemap == null)
        {
            Debug.LogWarning("No TileInfiniteCameraSpawner or ground tilemap found!");
            return;
        }
        Vector3 pos = transform.position;
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);
        int surfaceY = spawner.GetSurfaceY(x, z);

        Vector3Int cell = new Vector3Int(x, surfaceY, z);
        Vector3 worldSurface = groundTilemap.CellToWorld(cell);

        float offset = 1.1f;
        var col = GetComponent<Collider2D>();
        if (col != null) offset = col.bounds.extents.y + 0.1f;

        pos.x = worldSurface.x + groundTilemap.cellSize.x * 0.5f;
        pos.y = worldSurface.y + offset;
        pos.z = worldSurface.z;

        transform.position = pos;
        Debug.Log($"Player clamped to ground at {pos}");
    }

    // --- Fixed: Use worldArchiveManager.worldArchive ---
    void ResetAllCavesToUndiscovered()
    {
        if (spawner != null && spawner.enableWorldArchive
            && spawner.worldArchiveManager != null
            && spawner.worldArchiveManager.worldArchive != null)
        {
            var archive = spawner.worldArchiveManager.worldArchive;
            bool changed = false;
            foreach (var pair in archive.AllTiles())
            {
                TileData tileData = pair.Value;
                if (tileData != null && tileData.blockTagOrName == "cave" && tileData.discovered)
                {
                    tileData.discovered = false;
                    archive.SetTile(pair.Key, tileData);
                    changed = true;
                }
            }
            if (changed)
                archive.SaveAll();
        }
    }
}