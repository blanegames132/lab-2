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
    public TileInfiniteCameraSpawner worldSpawner;
    public TileHiddenSet tileHiddenSet;
    public Camera mainCamera;

    [Header("Fog Appearance")]
    public Color fogColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);

    [Header("Logic")]
    public bool enableHideLogic = true;
    [SerializeField] private bool reverseNextToAir = false; // Independent per GameObject, private but visible in Inspector
    public int cameraBuffer = 5;
    public int maxTilesPerFrame = 12;
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

        Vector3 camCenter = mainCamera.transform.position;
        int minX = Mathf.FloorToInt(camCenter.x - fogRadius - cameraBuffer);
        int maxX = Mathf.CeilToInt(camCenter.x + fogRadius + cameraBuffer);
        int minY = Mathf.FloorToInt(camCenter.y - fogRadius - cameraBuffer);
        int maxY = Mathf.CeilToInt(camCenter.y + fogRadius + cameraBuffer);
        BoundsInt camBounds = new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);

        if (camBounds != lastCameraBounds)
        {
            lastCameraBounds = camBounds;
            UpdateFogOverlay();
        }
    }

    void UpdateFogOverlay()
    {
        Vector3 targetPos = targetTilemap.transform.position;
        Vector3 fogPos = fogTilemap.transform.position;
        float desiredZ = targetPos.z + 0.1f;
        if (!Mathf.Approximately(fogPos.z, desiredZ))
            fogTilemap.transform.position = new Vector3(fogPos.x, fogPos.y, desiredZ);

        Vector3 camCenter = mainCamera.transform.position;
        int minX = Mathf.FloorToInt(camCenter.x - fogRadius - cameraBuffer);
        int maxX = Mathf.CeilToInt(camCenter.x + fogRadius + cameraBuffer);
        int minY = Mathf.FloorToInt(camCenter.y - fogRadius - cameraBuffer);
        int maxY = Mathf.CeilToInt(camCenter.y + fogRadius + cameraBuffer);

        BoundsInt bounds = targetTilemap.cellBounds;

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
                    if (dist > fogRadius) continue;

                    if (enableHideLogic && toHide.Contains(tile)) continue;

                    Vector3Int fogCell = fogTilemap.WorldToCell(worldPos);
                    newFogTiles.Add(fogCell);
                }

        // --- Only remove tiles next to air if toggle is OFF ---
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
        // If reverseNextToAir is ON, nothing is removed. Everything stays overlayed.

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