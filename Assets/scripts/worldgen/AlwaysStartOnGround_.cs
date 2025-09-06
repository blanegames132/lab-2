using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Ensures the GameObject always starts at the cell directly above the true ground surface
/// (the topmost tile of the ground tilemap, or predicted by SurfaceFinder),
/// and resets all discovered caves to undiscovered.
/// Uses TileEventBus for archive operations.
/// </summary>
public class AlwaysStartOnGround : MonoBehaviour
{
    [SerializeField] private SurfaceFinder surfaceFinder; // Reference to SurfaceFinder ScriptableObject
    [SerializeField] private Tilemap groundTilemap;

    [Header("Surface Search Settings")]
    public int surfaceSearchMaxY = 128; // Highest Y to scan
    public int surfaceSearchMinY = -128; // Lowest Y to scan

    void Start()
    {
        if (!surfaceFinder) surfaceFinder = FindObjectOfType<SurfaceFinder>();
        if (!groundTilemap) groundTilemap = surfaceFinder ? surfaceFinder.DefaultGroundTilemap : FindObjectOfType<Tilemap>();

        ClampToSurfaceFinderAtStart();
        ResetAllCavesToUndiscovered();
    }

    void ClampToSurfaceFinderAtStart()
    {
        if (surfaceFinder == null || groundTilemap == null)
        {
            Debug.LogWarning("SurfaceFinder or groundTilemap not found!");
            return;
        }

        Vector3 pos = transform.position;
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);

        // Find the highest spawned/predicted ground cell
        Vector3Int surfaceCell = surfaceFinder.GetSurfaceCell(x, z, surfaceSearchMaxY, surfaceSearchMinY, groundTilemap);

        // Move player to the cell directly above the surface
        Vector3Int playerCell = new Vector3Int(surfaceCell.x, surfaceCell.y + 1, surfaceCell.z);
        Vector3 worldSurface = groundTilemap.CellToWorld(playerCell);

        float offset = 1.1f;
        var col = GetComponent<Collider2D>();
        if (col != null) offset = col.bounds.extents.y + 0.1f;

        pos.x = worldSurface.x + groundTilemap.cellSize.x * 0.5f;
        pos.y = worldSurface.y + offset;
        pos.z = worldSurface.z;

        transform.position = pos;
        Debug.Log($"Player clamped to ground at {pos} (cell {playerCell}, surface cell {surfaceCell})");
    }

    void ResetAllCavesToUndiscovered()
    {
        TileEventBus.BroadcastResetAllCavesToUndiscovered();
    }
}