using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Service for calculating which tiles are visible for a given tilemap and camera.
/// </summary>
public class TilemapVisibleAreaService : MonoBehaviour
{
    public HashSet<Vector3Int> GetVisibleTiles(Tilemap tilemap, Camera cam, int buffer)
    {
        HashSet<Vector3Int> visible = new HashSet<Vector3Int>();
        if (tilemap == null || cam == null) return visible;

        Vector3 camMin = cam.ViewportToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 camMax = cam.ViewportToWorldPoint(new Vector3(1, 1, cam.nearClipPlane));
        int minX = Mathf.FloorToInt(camMin.x) - buffer;
        int maxX = Mathf.CeilToInt(camMax.x) + buffer;
        int minY = Mathf.FloorToInt(camMin.y) - buffer;
        int maxY = Mathf.CeilToInt(camMax.y) + buffer;

        BoundsInt bounds = tilemap.cellBounds;
        for (int x = Mathf.Max(bounds.xMin, minX); x <= Mathf.Min(bounds.xMax - 1, maxX); x++)
        for (int y = Mathf.Max(bounds.yMin, minY); y <= Mathf.Min(bounds.yMax - 1, maxY); y++)
        for (int z = bounds.zMin; z < bounds.zMax; z++)
        {
            Vector3Int cell = new Vector3Int(x, y, z);
            if (tilemap.GetTile(cell) != null)
                visible.Add(cell);
        }
        return visible;
    }
}