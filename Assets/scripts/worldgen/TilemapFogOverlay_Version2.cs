using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class TilemapFogOverlay : MonoBehaviour
{
    [Header("References")]
    public Tilemap targetTilemap;
    public Tilemap fogTilemap;
    public TileBase fogTile;

    public Camera mainCamera;
 
    public int caveSurfaceY = 0; // Assign surfaceY as appropriate, or get dynamically

    [Header("Fog Appearance")]
    public Color fogColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

    [Header("Logic")]
    public bool enableHideLogic = true;
    [SerializeField] private bool reverseNextToAir = false;
    public int bufferTiles = 6; // renamed for clarity, matches chunk math!
    public float updateInterval = 0.2f;
    public float fogRadius = 20f;

    private static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
    {
        new Vector3Int(-1,-1,0), new Vector3Int(0,-1,0), new Vector3Int(1,-1,0),
        new Vector3Int(-1,0,0),                     new Vector3Int(1,0,0),
        new Vector3Int(-1,1,0), new Vector3Int(0,1,0), new Vector3Int(1,1,0)
    };

    private HashSet<Vector3Int> fogTilesSet = new HashSet<Vector3Int>();
    private BoundsInt lastCameraBounds;
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
        if (cooldownTimer < updateInterval) return;
        cooldownTimer = 0f;

        // --- USE THE SAME MATH AS YOUR INFINITE TILE SYSTEM ---
        Vector3 cellSize = targetTilemap.cellSize;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        int tilesVisibleX = Mathf.CeilToInt(camWidth / cellSize.x);
        int tilesVisibleY = Mathf.CeilToInt(camHeight / cellSize.y);

        int totalTilesX = tilesVisibleX + bufferTiles * 2;
        int totalTilesY = tilesVisibleY + bufferTiles * 2;

        Vector3 camCenter = mainCamera.transform.position;
        Vector3Int camCell = targetTilemap.WorldToCell(camCenter);

        int minX = camCell.x - (totalTilesX / 2);
        int maxX = camCell.x + (totalTilesX / 2);
        int minY = camCell.y - (totalTilesY / 2);
        int maxY = camCell.y + (totalTilesY / 2);

        BoundsInt camBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);

        if (camBounds != lastCameraBounds)
        {
            lastCameraBounds = camBounds;
            UpdateFogOverlay(camBounds, camCenter, cellSize);
        }
    }

    void UpdateFogOverlay(BoundsInt camBounds, Vector3 camCenter, Vector3 cellSize)
    {
        // Set fog tilemap Z layer just above target
        float desiredZ = targetTilemap.transform.position.z + 0.1f;
        Vector3 fogPos = fogTilemap.transform.position;
        if (!Mathf.Approximately(fogPos.z, desiredZ))
            fogTilemap.transform.position = new Vector3(fogPos.x, fogPos.y, desiredZ);

        BoundsInt bounds = targetTilemap.cellBounds;
        HashSet<Vector3Int> newFogTiles = new HashSet<Vector3Int>();

        Vector3 srcOrigin = targetTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + targetTilemap.transform.position;
        Vector3 fogOrigin = fogTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + fogTilemap.transform.position;
        Vector3 worldOffset = fogOrigin - srcOrigin;
        for (int x = Mathf.Max(bounds.xMin, camBounds.xMin); x <= Mathf.Min(bounds.xMax - 1, camBounds.xMax); x++)
            for (int y = Mathf.Max(bounds.yMin, camBounds.yMin); y <= Mathf.Min(bounds.yMax - 1, camBounds.yMax); y++)
                for (int z = bounds.zMin; z < bounds.zMax; z++)
                {
                    Vector3Int tile = new Vector3Int(x, y, z);
                    if (targetTilemap.GetTile(tile) == null) continue;

                    Vector3 worldPos = targetTilemap.CellToWorld(tile) + worldOffset;
                    float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.y), new Vector2(camCenter.x, camCenter.y));
                    // Use the same fogRadius logic as tile system (optionally scale with cell size for tiny tiles)
                    float tileRadius = fogRadius / cellSize.x;
                    if (Vector2.Distance(new Vector2(tile.x, tile.y), new Vector2(camBounds.center.x, camBounds.center.y)) > tileRadius)
                        continue;

                    Vector3Int fogCell = fogTilemap.WorldToCell(worldPos);
                    newFogTiles.Add(fogCell);
                }

        // Remove edge fog if desired
        if (!reverseNextToAir)
        {
            var tilesToRemove = new List<Vector3Int>();
            foreach (var tile in newFogTiles)
            {
                bool isNextToAir = false;
                foreach (var offset in neighborOffsets)
                {
                    Vector3Int neighborPos = tile + offset;
                    if (targetTilemap.GetTile(neighborPos) == null)
                    {
                        isNextToAir = true;
                        break;
                    }
                }
                if (isNextToAir)
                {
                    tilesToRemove.Add(tile);
                }
            }
            foreach (var tile in tilesToRemove)
            {
                newFogTiles.Remove(tile);
            }
        }

        // Update fog tiles
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

    // Overload for Start
    void UpdateFogOverlay()
    {
        if (mainCamera == null || targetTilemap == null) return;
        Vector3 cellSize = targetTilemap.cellSize;
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        int tilesVisibleX = Mathf.CeilToInt(camWidth / cellSize.x);
        int tilesVisibleY = Mathf.CeilToInt(camHeight / cellSize.y);

        int totalTilesX = tilesVisibleX + bufferTiles * 2;
        int totalTilesY = tilesVisibleY + bufferTiles * 2;

        Vector3 camCenter = mainCamera.transform.position;
        Vector3Int camCell = targetTilemap.WorldToCell(camCenter);

        int minX = camCell.x - (totalTilesX / 2);
        int maxX = camCell.x + (totalTilesX / 2);
        int minY = camCell.y - (totalTilesY / 2);
        int maxY = camCell.y + (totalTilesY / 2);

        BoundsInt camBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
        UpdateFogOverlay(camBounds, camCenter, cellSize);
    }
}