using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Always overlays fog 0.1 units in front of the back tilemap,
/// no matter the tilemap's Z position or cell.z values.
/// Fog overlays all active tiles in the back tilemap.
/// </summary>
public class BackTilemapFogOverlay : MonoBehaviour
{
    [Header("References")]
    public Tilemap backTilemap;
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

    private static readonly Vector3Int[] neighborOffsets = new Vector3Int[]
    {
        new Vector3Int(-1,-1,0), new Vector3Int(0,-1,0), new Vector3Int(1,-1,0),
        new Vector3Int(-1,0,0),                       new Vector3Int(1,0,0),
        new Vector3Int(-1,1,0), new Vector3Int(0,1,0), new Vector3Int(1,1,0)
    };

    void Start()
    {
        if (fogTilemap != null)
        {
            var collider = fogTilemap.GetComponent<TilemapCollider2D>();
            if (collider != null) collider.enabled = false;
        }
    }

    void Update()
    {
        if (backTilemap == null || fogTilemap == null || fogTile == null ||
            worldSpawner == null || tileHiddenSet == null || mainCamera == null)
            return;

        Vector3 backPos = backTilemap.transform.position;
        Vector3 fogPos = fogTilemap.transform.position;
        float desiredZ = backPos.z + 0.1f;
        if (!Mathf.Approximately(fogPos.z, desiredZ))
            fogTilemap.transform.position = new Vector3(fogPos.x, fogPos.y, desiredZ);

        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        int minX = Mathf.FloorToInt(camMin.x) - cameraBuffer;
        int maxX = Mathf.CeilToInt(camMax.x) + cameraBuffer;
        int minY = Mathf.FloorToInt(camMin.y) - cameraBuffer;
        int maxY = Mathf.CeilToInt(camMax.y) + cameraBuffer;

        Vector3 srcOrigin = backTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + backTilemap.transform.position;
        Vector3 fogOrigin = fogTilemap.layoutGrid.CellToWorld(Vector3Int.zero) + fogTilemap.transform.position;
        Vector3 worldOffset = fogOrigin - srcOrigin;

        HashSet<Vector3Int> activeTiles = worldSpawner.GetActiveTilesForTilemap(backTilemap);
        HashSet<Vector3Int> toHide = tileHiddenSet.GetTilesToHide(backTilemap.transform.position);

        fogTilemap.ClearAllTiles();
        fogTilemap.color = fogColor;

        foreach (var tile in activeTiles)
        {
            if (tile.x < minX || tile.x > maxX || tile.y < minY || tile.y > maxY)
                continue;
            if (backTilemap.GetTile(tile) == null)
                continue;

            Vector3 worldPos = backTilemap.CellToWorld(tile) + worldOffset;
            Vector3Int fogCell = fogTilemap.WorldToCell(worldPos);

            if (enableHideLogic)
            {
                if (toHide.Contains(tile)) continue;
                if (hideIfNextToAir)
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
            fogTilemap.SetTile(fogCell, fogTile);
            fogTilemap.SetColliderType(fogCell, Tile.ColliderType.None);
        }
    }
}