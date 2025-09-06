using UnityEngine;
using UnityEngine.Tilemaps;

public class AlwaysStartOnGround : MonoBehaviour
{
    [SerializeField] private InfiniteCameraSpawnerModular spawner;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private ChunkedWorldArchive worldArchive;

    void Start()
    {
        if (!spawner) spawner = FindObjectOfType<InfiniteCameraSpawnerModular>();
        if (!groundTilemap) groundTilemap = FindObjectOfType<Tilemap>();
        if (worldArchive == null && spawner != null && spawner.worldArchiveManager != null)
            worldArchive = spawner.worldArchiveManager.worldArchive;
        ClampToNearestGroundAtStart();
        ResetAllCavesToUndiscovered();
    }

    void ClampToNearestGroundAtStart()
    {
        if (spawner == null || groundTilemap == null)
        {
            Debug.LogWarning("No spawner or ground tilemap found!");
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

    void ResetAllCavesToUndiscovered()
    {
        if (worldArchive == null)
            return;

        var allTiles = worldArchive.AllTiles();
        bool changed = false;
        foreach (var pair in allTiles)
        {
            TileData tileData = pair.Value;
            if (tileData != null && tileData.blockTagOrName == "cave" && tileData.discovered)
            {
                tileData.discovered = false;
                worldArchive.SetTile(pair.Key, tileData);
                changed = true;
            }
        }
        if (changed)
            worldArchive.SaveAll();
    }
}