using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Finds the surface for any tilemap layer, using either actual spawned tiles or optional generation prediction.
/// Draws a gizmo at the found/predicted surface position.
/// </summary>
public class SurfaceFinder : MonoBehaviour
{
    [Tooltip("Tilemaps to search for surface, from highest priority to lowest.")]
    public List<Tilemap> surfaceTilemaps = new List<Tilemap>();

    [Tooltip("Ground Tilemap (optional, for FindObjectOfType fallback).")]
    public Tilemap DefaultGroundTilemap;

    [Header("Generation Parameters (optional for prediction)")]
    public float hillNoiseScale = 0.08f;
    public float cliffSharpness = 2.0f;
    public float hillHeight = 12f;
    public float hillVerticalShift = 0f;
    public float canyonThreshold = 0.9f;
    public int groundSeed = 101;
    public float groundZOffset = 0f;

    public TileBase defaultAirTile;

    /// <summary>
    /// Returns the cell of the highest non-air, non-null tile at (x, z) in the specified tilemap, from top to bottom.
    /// If no tile exists yet, returns the cell where the surface block will spawn using prediction logic.
    /// </summary>
    public Vector3Int GetSurfaceCell(int x, int z, int maxY, int minY, Tilemap specificTilemap = null)
    {
        Tilemap searchTilemap = specificTilemap ? specificTilemap : (surfaceTilemaps.Count > 0 ? surfaceTilemaps[0] : null);
        if (searchTilemap != null)
        {
            for (int y = maxY; y >= minY; y--)
            {
                Vector3Int cell = new Vector3Int(x, y, z);
                TileBase tile = searchTilemap.GetTile(cell);
                if (tile != null && tile != defaultAirTile)
                    return cell;
            }
        }
        // If not found, predict using generation parameters
        return PredictSurfaceCell(x, z);
    }

    /// <summary>
    /// Predicts the surface cell using generation math (matches your world hill curve logic).
    /// </summary>
    public Vector3Int PredictSurfaceCell(int x, int z)
    {
        int tilemapZ = z + Mathf.RoundToInt(groundZOffset);
        System.Random rand = new System.Random(groundSeed + tilemapZ * 999);
        float noiseScale = hillNoiseScale * (1f + (rand.Next(-10, 10) * 0.01f));
        float cliff = cliffSharpness * (1f + (rand.Next(-10, 10) * 0.01f));
        float hHeight = hillHeight * (1f + (rand.Next(-10, 10) * 0.01f));
        float vShift = hillVerticalShift * (1f + (rand.Next(-10, 10) * 0.01f));
        float cThreshold = canyonThreshold * (1f + (rand.Next(-10, 10) * 0.01f));

        float hill = Mathf.PerlinNoise((x + groundSeed) * noiseScale, (tilemapZ + groundSeed) * noiseScale);
        hill = Mathf.Pow(hill, cliff);
        float canyonMask = Mathf.PerlinNoise((tilemapZ + groundSeed) * 0.12f, (x + groundSeed) * 0.015f);

        float height = hill * hHeight + vShift;
        if (canyonMask > cThreshold)
            height += hHeight * 2f;
        else if (canyonMask < 1f - cThreshold)
            height -= hHeight * 2f;

        int surfaceY = Mathf.RoundToInt(height);
        return new Vector3Int(x, surfaceY, tilemapZ);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw a gizmo at the predicted surface for the origin (0,0)
        if (DefaultGroundTilemap)
        {
            Vector3Int cell = GetSurfaceCell(0, 0, 128, -128, DefaultGroundTilemap);
            Vector3 worldPos = DefaultGroundTilemap.CellToWorld(cell) + new Vector3(DefaultGroundTilemap.cellSize.x * 0.5f, DefaultGroundTilemap.cellSize.y * 0.5f, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(worldPos, new Vector3(1, 1, 1));
        }
    }
}