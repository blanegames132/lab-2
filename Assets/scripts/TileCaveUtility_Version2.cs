using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileCaveUtility : MonoBehaviour
{
    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.09f;
    public float caveThreshold = 0.5f;

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;

    [Header("Tilemap References")]
    public List<Tilemap> tilemaps; // Assign all your tilemaps in the Inspector!

    public bool IsInitialized { get; private set; } = false;

    // Set your tilemap bounds here, or dynamically find them.
    public int minX = -100, maxX = 100, minY = -20, maxY = 30, z = 0;

    void Awake()
    {
        IsInitialized = true;
    }

    void Start()
    {
        if (tilemaps != null)
        {
            foreach (var tilemap in tilemaps)
            {
                if (tilemap == null) continue;
                ReplaceSurfaceTilesNextToAir(
                    tilemap,
                    minX, maxX, minY, maxY, z,
                    (x, y, z) => tilemap.GetTile(new Vector3Int(x, y, z)) == null
                );
            }
        }
    }

    /// <summary>
    /// Returns a cave noise sample at x, y, z. Use > caveThreshold for cave.
    /// </summary>
    public float CaveGenerator(int x, int y, int z)
    {
        float noiseXY = Mathf.PerlinNoise(x * caveFrequency, y * caveFrequency);
        float noiseYZ = Mathf.PerlinNoise(y * caveFrequency, z * caveFrequency);
        float noiseZX = Mathf.PerlinNoise(z * caveFrequency, x * caveFrequency);
        return (noiseXY + noiseYZ + noiseZX) / 3f;
    }

    /// <summary>
    /// Replace all surface tiles next to air with cave tiles.
    /// </summary>
    public void ReplaceSurfaceTilesNextToAir(Tilemap tilemap, int minX, int maxX, int minY, int maxY, int z, System.Func<int, int, int, bool> isAirFunc)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, z);

                // Only consider if this tile is solid/ground/etc.
                TileBase current = tilemap.GetTile(pos);
                if (current == null) continue;

                // Check all 4 neighbors (up, down, left, right) for air
                bool adjacentToAir =
                    isAirFunc(x + 1, y, z) ||
                    isAirFunc(x - 1, y, z) ||
                    isAirFunc(x, y + 1, z) ||
                    isAirFunc(x, y - 1, z);

                if (adjacentToAir)
                {
                    // Replace with cave tile
                    tilemap.SetTile(pos, visibleCaveTileAsset);
                }
            }
        }
    }
}