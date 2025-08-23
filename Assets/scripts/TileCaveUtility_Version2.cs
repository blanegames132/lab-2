using UnityEngine;
using UnityEngine.Tilemaps;
using System;

public class TileCaveUtility : MonoBehaviour
{
    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.09f;
    public float caveThreshold = 0.5f;
    [Tooltip("Controls horizontal stretching of caves. 1.0 = classic.")]
    public float caveHorizontalMultiplier = 1.0f;
    [Tooltip("Controls vertical stretching of caves. 1.0 = classic.")]
    public float caveVerticalMultiplier = 1.0f;
    [Tooltip("How sharp or smooth the cave boundaries are. 1.0 = classic.")]
    [Range(0.5f, 5f)]
    public float caveSharpness = 1.0f;
    [Tooltip("Add a secondary frequency for more layered noise.")]
    public float secondaryFrequency = 0.0f;
    [Tooltip("Weight for secondary noise layer.")]
    [Range(0f, 1f)]
    public float secondaryWeight = 0.0f;
    [Tooltip("Adds a bias to make caves more likely in lower or higher areas (-1 = bottom, +1 = top, 0 = no bias).")]
    [Range(-1f, 1f)]
    public float verticalBias = 0.0f;
    [Tooltip("Influence of the vertical bias (0 = none, 1 = strong).")]
    [Range(0f, 1f)]
    public float verticalBiasStrength = 0.0f;

    [Header("Cave Shape/Curve Controls")]
    [Tooltip("How much caves should curve/wave (0 = straight, higher = more curvy/wiggly).")]
    public float caveCurve = 0.0f;
    [Tooltip("How much caves get thinner/thicker as you go down (0 = fixed, >0 = thinner down, <0 = thicker down).")]
    public float caveVerticalScale = 0.0f;
    [Tooltip("How much caves get thinner/thicker as you go sideways (0 = fixed, >0 = thinner sides, <0 = thicker sides).")]
    public float caveHorizontalScale = 0.0f;

    [Header("Deep Cave Controls")]
    [Tooltip("How deep (in tiles) the 'deep cave' region is below the surface.")]
    public int surfaceDepth = 100; // Caves only spawn below surfaceY - surfaceDepth

    [Header("Cave Entrance Controls")]
    [Tooltip("Number of entrances per chunk (average). Higher = more entrances across the map.")]
    [Range(0f, 1f)]
    public float caveEntranceChance = 0.3f;
    [Tooltip("How much cave entrances zigzag (0 = straight down, higher = more zigzag/sideways)")]
    [Range(0f, 2f)]
    public float caveEntranceZigzag = 1.0f;
    [Tooltip("How wide cave entrances are (in tiles, 2 = fairly wide, 4 = very wide)")]
    [Range(1, 8)]
    public int caveEntranceWidth = 3;

    public int caveEntranceAbove = 10;
    public int caveEntranceBelow = 20;

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;

    public bool IsInitialized { get; private set; } = false;

    public int minX = -100, maxX = 100, minY = -120, maxY = 30, z = 0;

    void Awake()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Returns true if a cave or cave entrance is present at this location.
    /// </summary>
    public bool IsCaveAt(int x, int y, int z, int surfaceY)
    {
        return IsCaveEntrance(x, y, z, surfaceY) || CaveGenerator(x, y, z, surfaceY) > caveThreshold;
    }

    /// <summary>
    /// Returns true if this (x, y, z) is part of a cave entrance from 10 above to 20 below surface.
    /// Entrances are wider, zigzag, and have a per-column chance to appear.
    /// </summary>
    public bool IsCaveEntrance(int x, int y, int z, int surfaceY)
    {
        int entranceTop = surfaceY + caveEntranceAbove;
        int entranceBottom = surfaceY - caveEntranceBelow;
        if (y > entranceTop || y < entranceBottom) return false;

        // Each (x,z) column gets a stable, repeatable chance for an entrance, controlled by caveEntranceChance.
        int entranceSeed = x * 73856093 ^ z * 19349663;
        System.Random entranceRand = new System.Random(entranceSeed);

        // Stable PRNG for repeatable per-column entrances
        if (entranceRand.NextDouble() >= caveEntranceChance)
            return false;

        // The entrance gets a base center x, with smooth zigzag/diagonal drift as it goes down
        float baseSlope = Mathf.Lerp(-0.5f, 0.5f, (float)entranceRand.NextDouble()); // some diagonal drift
        float centerX = x + baseSlope * (y - entranceTop);

        // Add sine-based zigzag on top of drift, so the entrance is not straight down
        float zigzag = Mathf.Sin((y - entranceTop) * 0.4f + entranceSeed) * caveEntranceZigzag * (caveEntranceBelow + caveEntranceAbove) * 0.5f;
        centerX += zigzag;

        // Entrances are wide bands, not just a single tile
        if (Mathf.Abs(x - centerX) <= (caveEntranceWidth / 2f))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns a cave noise sample at x, y, z, using the provided surfaceY for this column.
    /// Only deep caves: caves spawn only below (surfaceY - surfaceDepth).
    /// </summary>
    public float CaveGenerator(int x, int y, int z, int surfaceY)
    {
        int caveStartY = surfaceY - surfaceDepth;

        if (y > caveStartY)
            return 0f;

        // --- Curviness/warping ---
        float xCurve = x;
        float yCurve = y;
        if (caveCurve != 0f)
        {
            xCurve += Mathf.Sin(y * 0.1f) * caveCurve * 10f;
            yCurve += Mathf.Sin(x * 0.1f + 100f) * caveCurve * 10f;
        }

        // --- Main noise, with stretching ---
        float fx = xCurve * caveFrequency * caveHorizontalMultiplier;
        float fy = yCurve * caveFrequency * caveVerticalMultiplier;
        float fz = z * caveFrequency * 0.7f;

        float noiseXY = Mathf.PerlinNoise(fx, fy);
        float noiseYZ = Mathf.PerlinNoise(fy, fz);
        float noiseZX = Mathf.PerlinNoise(fz, fx);
        float noise = (noiseXY + noiseYZ + noiseZX) / 3f;

        // --- Secondary noise layer ---
        if (secondaryFrequency > 0f && secondaryWeight > 0f)
        {
            float sfx = xCurve * secondaryFrequency * caveHorizontalMultiplier;
            float sfy = yCurve * secondaryFrequency * caveVerticalMultiplier;
            float sfz = z * secondaryFrequency * 0.7f;
            float snoiseXY = Mathf.PerlinNoise(sfx, sfy);
            float snoiseYZ = Mathf.PerlinNoise(sfy, sfz);
            float snoiseZX = Mathf.PerlinNoise(sfz, sfx);
            float snoise = (snoiseXY + snoiseYZ + snoiseZX) / 3f;
            noise = Mathf.Lerp(noise, snoise, secondaryWeight);
        }

        // --- Sharpness adjustment ---
        if (caveSharpness != 1f)
            noise = Mathf.Pow(noise, caveSharpness);

        // --- Vertical bias (more caves at top/bottom) ---
        if (verticalBiasStrength > 0f && verticalBias != 0f)
        {
            float yNorm = Mathf.InverseLerp(minY, caveStartY, y) * 2 - 1;
            float bias = 1f - Mathf.Abs(yNorm - verticalBias);
            noise = Mathf.Lerp(noise, noise * bias, verticalBiasStrength);
        }

        // --- Vertical scale (caves get thinner/thicker as you go down) ---
        if (caveVerticalScale != 0f)
        {
            float yNormalized = Mathf.InverseLerp(minY, caveStartY, y);
            float scale = Mathf.Lerp(1f, 0.25f, yNormalized * caveVerticalScale);
            noise *= scale;
        }

        // --- Horizontal scale (caves get thinner/thicker as you go sideways) ---
        if (caveHorizontalScale != 0f)
        {
            float xNormalized = Mathf.InverseLerp(minX, maxX, x);
            float scale = Mathf.Lerp(1f, 0.25f, xNormalized * caveHorizontalScale);
            noise *= scale;
        }

        return noise;
    }

    /// <summary>
    /// Fills the tilemap with caves using the noise function and caveThreshold.
    /// This will always spawn cave tiles where a cave is present and clear non-cave tiles, so cave has priority.
    /// </summary>
    public void GenerateCaves(Tilemap tilemap, System.Func<int, int, int> getSurfaceY)
    {
        Vector3Int pos = new Vector3Int();
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = 0; z < 1; z++)
            {
                int surfaceY = getSurfaceY(x, z);
                pos.x = x;
                pos.z = z;
                for (int y = minY; y <= maxY; y++)
                {
                    pos.y = y;
                    bool cave = IsCaveAt(x, y, z, surfaceY);
                    if (cave)
                        tilemap.SetTile(pos, visibleCaveTileAsset); // Always set cave tile if cave or entrance
                    else
                        tilemap.SetTile(pos, null); // Always clear to air if not a cave
                }
            }
        }
    }
}