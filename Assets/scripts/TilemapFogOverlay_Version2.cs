using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Highly reusable, optimized tilemap fog overlay.
/// Attach multiple times, configure each for a different tilemap overlay in the Inspector.
/// Utility functions included.
/// </summary>
[DisallowMultipleComponent]
public class TilemapFogOverlay : MonoBehaviour
{
    [Header("References")]
    public Tilemap targetTilemap;
    public Tilemap fogTilemap;
    public TileBase fogTile;
    public TileInfiniteCameraSpawner worldSpawner;
    public TileHiddenSet tileHiddenSet;
    public Camera mainCamera;

    [Header("Fog Appearance")]
    public Color fogColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

    [Header("Logic")]
    public bool enableHideLogic = true;
    public bool hideIfNextToAir = true;
    public int cameraBuffer = 5;
    public int maxTilesPerFrame = 12; // Batch size for fog updates
    public float updateInterval = 0.2f; // Minimum time between batch updates
    public float fogRadius = 20f; // Area where fog is always present

    // Utility: Overlay Z logic
    void OverlayFogAboveTarget()
    {
        Vector3 targetPos = targetTilemap.transform.position;
        Vector3 fogPos = fogTilemap.transform.position;
        float desiredZ = targetPos.z + 0.1f;
        if (!Mathf.Approximately(fogPos.z, desiredZ))
            fogTilemap.transform.position = new Vector3(fogPos.x, fogPos.y, desiredZ);
    }

    // Utility: Initial full fog cover
    void CoverAllWithFog()
    {
        BoundsInt bounds = targetTilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (targetTilemap.GetTile(pos) != null)
            {
                fogTilemap.SetTile(pos, fogTile);
            }
        }
    }

    // Utility: Adjacency check
    bool IsNextToAir(Vector3Int tile)
    {
        if (worldSpawner == null) return false;
        foreach (var offset in neighborOffsets)
        {
            Vector3Int neighborPos = tile + offset;
            TileType neighborType = worldSpawner.GetTileTypeForFog(neighborPos, TileType.Dirt);
            if (neighborType == TileType.Air) return true;
        }
        return false;
    }

    // Utility: Get all tiles within a 20 radius of a world position (not used in main)
    HashSet<Vector3Int> TilesInRadius(Vector3 worldCenter, float radius)
    {
        HashSet<Vector3Int> result = new HashSet<Vector3Int>();
        BoundsInt bounds = targetTilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
            for (int y = bounds.yMin; y < bounds.yMax; y++)
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int cell = new Vector3Int(x, y, z);
                    Vector3 cellWorld = targetTilemap.CellToWorld(cell);
                    if (Vector2.Distance(new Vector2(cellWorld.x, cellWorld.y), new Vector2(worldCenter.x, worldCenter.y)) <= radius)
                        result.Add(cell);
                }
        return result;
    }

    private static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
    {
        new Vector3Int(-1,-1,0), new Vector3Int(0,-1,0), new Vector3Int(1,-1,0),
        new Vector3Int(-1,0,0),                     new Vector3Int(1,0,0),
        new Vector3Int(-1,1,0), new Vector3Int(0,1,0), new Vector3Int(1,1,0)
    };

    private HashSet<Vector3Int> fogTilesSet = new HashSet<Vector3Int>();
    private BoundsInt lastCameraBounds;
    private float updateCooldown = 0.1f;
    private float cooldownTimer = 0f;

    void Start()
    {
        if (fogTilemap != null)
        {
            var collider = fogTilemap.GetComponent<TilemapCollider2D>();
            if (collider != null) collider.enabled = false;
        }
        lastCameraBounds = new BoundsInt(int.MinValue, int.MinValue, 0, 0, 0, 1);

        // Initial update to draw fog
        UpdateFogOverlay();
    }

    void Update()
    {
        if (targetTilemap == null || fogTilemap == null || fogTile == null || mainCamera == null) return;

        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < updateCooldown) return;
        cooldownTimer = 0f;

        // Calculate camera bounds with buffer (behind means expand by buffer)
        Vector3 camCenter = mainCamera.transform.position;
        int minX = Mathf.FloorToInt(camCenter.x - fogRadius - cameraBuffer);
        int maxX = Mathf.CeilToInt(camCenter.x + fogRadius + cameraBuffer);
        int minY = Mathf.FloorToInt(camCenter.y - fogRadius - cameraBuffer);
        int maxY = Mathf.CeilToInt(camCenter.y + fogRadius + cameraBuffer);
        BoundsInt camBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);

        // Only update if camera bounds changed
        if (camBounds != lastCameraBounds)
        {
            lastCameraBounds = camBounds;
            UpdateFogOverlay();
        }
    }

    void UpdateFogOverlay()
    {
        // 1. Move fog tilemap to overlay just in front
        Vector3 targetPos = targetTilemap.transform.position;
        Vector3 fogPos = fogTilemap.transform.position;
        float desiredZ = targetPos.z + 0.1f;
        if (!Mathf.Approximately(fogPos.z, desiredZ))
            fogTilemap.transform.position = new Vector3(fogPos.x, fogPos.y, desiredZ);

        // 2. Calculate area for fog (buffer behind radius)
        Vector3 camCenter = mainCamera.transform.position;
        int minX = Mathf.FloorToInt(camCenter.x - fogRadius - cameraBuffer);
        int maxX = Mathf.CeilToInt(camCenter.x + fogRadius + cameraBuffer);
        int minY = Mathf.FloorToInt(camCenter.y - fogRadius - cameraBuffer);
        int maxY = Mathf.CeilToInt(camCenter.y + fogRadius + cameraBuffer);

        BoundsInt bounds = targetTilemap.cellBounds;

        // 3. Build list of which tiles should have fog (all within fogRadius area!)
        HashSet<Vector3Int> newFogTiles = new HashSet<Vector3Int>();
        Vector3 srcOrigin = targetTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + targetTilemap.transform.position;
        Vector3 fogOrigin = fogTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + fogTilemap.transform.position;
        Vector3 worldOffset = fogOrigin - srcOrigin;

        HashSet<Vector3Int> toHide = tileHiddenSet ? tileHiddenSet.GetTilesToHide(targetTilemap.transform.position) : new HashSet<Vector3Int>();
        for (int x = Mathf.Max(bounds.xMin, minX); x <= Mathf.Min(bounds.xMax - 1, maxX); x++)
            for (int y = Mathf.Max(bounds.yMin, minY); y <= Mathf.Min(bounds.yMax - 1, maxY); y++)
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int tile = new Vector3Int(x, y, z);
                    if (targetTilemap.GetTile(tile) == null) continue;

                    Vector3 worldPos = targetTilemap.CellToWorld(tile) + worldOffset;
                    float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(camCenter.x, camCenter.y));
                    if (dist > fogRadius) continue; // Only fog inside the radius

                    if (enableHideLogic)
                    {
                        // Reveal (remove fog) if tile is in the hide set, otherwise keep fog!
                        if (toHide.Contains(tile))
                        {
                            continue; // Do not add fog for revealed tiles
                        }
                        if (hideIfNextToAir && worldSpawner != null)
                        {
                            bool adjacentToAir = false;
                            foreach (var offset in neighborOffsets)
                            {
                                Vector3Int neighborPos = tile + offset;
                                TileType neighborType = worldSpawner.GetTileTypeForFog(neighborPos, TileType.Dirt);
                                if (neighborType == TileType.Air) { adjacentToAir = true; break; }
                            }
                            if (adjacentToAir) continue; // Reveal if adjacent to air
                        }
                    }

                    Vector3Int fogCell = fogTilemap.WorldToCell(worldPos);
                    newFogTiles.Add(fogCell);
                }

        // 4. Remove fog from old, add to new only if changed (delta update)
        foreach (var pos in fogTilesSet)
        {
            if (!newFogTiles.Contains(pos))
                fogTilemap.SetTile(pos, null);
        }
        foreach (var pos in newFogTiles)
        {
            if (!fogTilesSet.Contains(pos))
            {
                fogTilemap.SetTile(pos, fogTile);
                fogTilemap.SetColliderType(pos, Tile.ColliderType.None);
            }
        }
        fogTilemap.color = fogColor;
        fogTilesSet = newFogTiles;
    }
}