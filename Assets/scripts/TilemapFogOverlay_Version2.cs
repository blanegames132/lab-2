using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Highly reusable, optimized tilemap fog overlay.
/// Attach multiple times, configure each for a different tilemap overlay in the Inspector.
/// </summary>
[DisallowMultipleComponent]
public class TilemapFogOverlay : MonoBehaviour
{
    [Header("References")]
    public Tilemap targetTilemap;    // The tilemap to overlay fog on (e.g., front, middleFront, etc.)
    public Tilemap fogTilemap;       // The tilemap to draw fog on (should match grid/size)
    public TileBase fogTile;         // The fog visual tile asset
    public TileInfiniteCameraSpawner worldSpawner; // For tile type logic (assign if using hideIfNextToAir)
    public TileHiddenSet tileHiddenSet;            // For hide logic (optional)
    public Camera mainCamera;                      // Camera for bounds

    [Header("Fog Appearance")]
    public Color fogColor = new Color(0.1f,0.1f,0.1f,0.6f);

    [Header("Logic")]
    public bool enableHideLogic = true;
    public bool hideIfNextToAir = true;
    public int cameraBuffer = 5;

    private static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
    {
        new Vector3Int(-1,-1,0), new Vector3Int(0,-1,0), new Vector3Int(1,-1,0),
        new Vector3Int(-1,0,0),                     new Vector3Int(1,0,0),
        new Vector3Int(-1,1,0), new Vector3Int(0,1,0), new Vector3Int(1,1,0)
    };

    // Optimization fields
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
        UpdateFogOverlay();
    }

    void Update()
    {
        if (targetTilemap == null || fogTilemap == null || fogTile == null || mainCamera == null) return;

        cooldownTimer += Time.deltaTime;
        if (cooldownTimer < updateCooldown) return;
        cooldownTimer = 0f;

        // Camera bounds
        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        int minX = Mathf.FloorToInt(camMin.x) - cameraBuffer;
        int maxX = Mathf.CeilToInt(camMax.x) + cameraBuffer;
        int minY = Mathf.FloorToInt(camMin.y) - cameraBuffer;
        int maxY = Mathf.CeilToInt(camMax.y) + cameraBuffer;
        BoundsInt camBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);

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

        // 2. Calculate visible area
        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        int minX = Mathf.FloorToInt(camMin.x) - cameraBuffer;
        int maxX = Mathf.CeilToInt(camMax.x) + cameraBuffer;
        int minY = Mathf.FloorToInt(camMin.y) - cameraBuffer;
        int maxY = Mathf.CeilToInt(camMax.y) + cameraBuffer;

        BoundsInt bounds = targetTilemap.cellBounds;

        // 3. Build list of which tiles should have fog
        HashSet<Vector3Int> newFogTiles = new HashSet<Vector3Int>();
        Vector3 srcOrigin = targetTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + targetTilemap.transform.position;
        Vector3 fogOrigin = fogTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + fogTilemap.transform.position;
        Vector3 worldOffset = fogOrigin - srcOrigin;

        HashSet<Vector3Int> toHide = tileHiddenSet ? tileHiddenSet.GetTilesToHide(targetTilemap.transform.position) : new HashSet<Vector3Int>();
        for (int x = Mathf.Max(bounds.xMin, minX); x <= Mathf.Min(bounds.xMax-1, maxX); x++)
        for (int y = Mathf.Max(bounds.yMin, minY); y <= Mathf.Min(bounds.yMax-1, maxY); y++)
        for (int z = bounds.zMin; z < bounds.zMax; z++)
        {
            Vector3Int tile = new Vector3Int(x, y, z);
            if (targetTilemap.GetTile(tile) == null) continue;

            if (enableHideLogic)
            {
                if (toHide.Contains(tile)) continue;
                if (hideIfNextToAir && worldSpawner != null)
                {
                    bool adjacentToAir = false;
                    foreach (var offset in neighborOffsets)
                    {
                        Vector3Int neighborPos = tile + offset;
                        TileType neighborType = worldSpawner.GetTileTypeForFog(neighborPos, TileType.Dirt);
                        if (neighborType == TileType.Air) { adjacentToAir = true; break; }
                    }
                    if (adjacentToAir) continue;
                }
            }

            Vector3 worldPos = targetTilemap.CellToWorld(tile) + worldOffset;
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