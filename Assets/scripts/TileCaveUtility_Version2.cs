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
    [Tooltip("Chance for a column to have a cave entrance (0-1)")]
    [Range(0f, 1f)]
    public float caveEntranceChance = 0.3f;
    [Tooltip("How much cave entrances zigzag (0 = straight, higher = more zigzag/sideways)")]
    [Range(0f, 2f)]
    public float caveEntranceZigzag = 1.0f;
    [Tooltip("How wide cave entrances are (in tiles, 2 = minimum allowed)")]
    [Range(2, 8)]
    public int caveEntranceWidth = 2;

    public int caveEntranceAbove = 10;
    public int caveEntranceBelow = 20;

    // END points for left and right cave exits at the cave bottom
    public int caveExitYOffset = 50; // vertical drop before exit
    public int caveExitXOffset = 60; // horizontal offset from start x (left/right)

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;

    [Tooltip("Tilemap to spawn cave assets into. Assign in Inspector.")]
    public Tilemap targetTilemap;
    public Transform playerTransform; // Assign the player transform in Inspector
    public bool IsInitialized { get; private set; } = false;

    public int minX = -100, maxX = 100, minY = -120, maxY = 30, z = 0;

    [Header("Debug Controls")]
    public bool debug = true; // Toggle this in Inspector to control cave drawing/deletion
    public int targetZLayer = 0; // Set this to match your desired z-layer for cave drawing

    void Awake()
    {
        IsInitialized = true;
    }

    /// <summary>
    /// Returns true if a cave or cave entrance is present at this location.
    /// </summary>
    public bool IsCaveAt(int x, int y, int z, int surfaceY)
    {
        // Check horizontally for entrance
        for (int dx = -caveEntranceWidth / 2; dx <= caveEntranceWidth / 2; dx++)
        {
            if (IsCaveEntranceSingle(x + dx, y, z, surfaceY) &&
                (IsCaveEntranceSingle(x + dx, y + 1, z, surfaceY) || IsCaveEntranceSingle(x + dx, y - 1, z, surfaceY)))
            {
                return true;
            }
        }
        // Otherwise, check for regular cave
        return CaveGenerator(x, y, z, surfaceY) > caveThreshold;
    }

    public bool IsCaveEntranceSingle(int x, int y, int z, int surfaceY)
    {
        int entranceTop = surfaceY + caveEntranceAbove;
        int entranceBottom = surfaceY - caveEntranceBelow;
        if (y > entranceTop || y < entranceBottom - caveExitYOffset) return false;

        // Stable PRNG per column
        int entranceSeed = x * 73856093 ^ z * 19349663;
        System.Random entranceRand = new System.Random(entranceSeed);

        if (entranceRand.NextDouble() >= caveEntranceChance)
            return false;

        // Pick left or right as the cave exit for this entrance (random for each entrance column)
        bool exitLeft = entranceRand.NextDouble() < 0.5;

        // The Y coordinate of the bottom exit
        int exitY = entranceBottom - caveExitYOffset;
        // The X coordinate of the bottom exit
        int exitX = x + (exitLeft ? -caveExitXOffset : caveExitXOffset);

        // Parametric t for [entranceTop ... exitY]
        float t = Mathf.InverseLerp(entranceTop, exitY, y);
        float curvePower = 1.7f; // Controls how long it stays horizontal
        float curvedT = Mathf.Pow(t, curvePower);

        float idealX = Mathf.Lerp(x, exitX, curvedT);

        float zigzag = Mathf.Sin((y - entranceTop) * 0.7f + entranceSeed) * caveEntranceZigzag * (caveEntranceBelow + caveEntranceAbove + caveExitYOffset) * 0.45f * Mathf.Clamp01(t + 0.1f);
        float centerX = idealX + zigzag;

        return Mathf.Abs(x - centerX) <= (caveEntranceWidth / 2f);
    }

    public float CaveGenerator(int x, int y, int z, int surfaceY)
    {
        int caveStartY = surfaceY - surfaceDepth;

        if (y > caveStartY)
            return 0f;

        float xCurve = x;
        float yCurve = y;
        if (caveCurve != 0f)
        {
            xCurve += Mathf.Sin(y * 0.1f) * caveCurve * 10f;
            yCurve += Mathf.Sin(x * 0.1f + 100f) * caveCurve * 10f;
        }

        float fx = xCurve * caveFrequency * caveHorizontalMultiplier;
        float fy = yCurve * caveFrequency * caveVerticalMultiplier;
        float fz = z * caveFrequency * 0.7f;

        float noiseXY = Mathf.PerlinNoise(fx, fy);
        float noiseYZ = Mathf.PerlinNoise(fy, fz);
        float noiseZX = Mathf.PerlinNoise(fz, fx);
        float noise = (noiseXY + noiseYZ + noiseZX) / 3f;

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

        if (caveSharpness != 1f)
            noise = Mathf.Pow(noise, caveSharpness);

        if (verticalBiasStrength > 0f && verticalBias != 0f)
        {
            float yNorm = Mathf.InverseLerp(minY, caveStartY, y) * 2 - 1;
            float bias = 1f - Mathf.Abs(yNorm - verticalBias);
            noise = Mathf.Lerp(noise, noise * bias, verticalBiasStrength);
        }

        if (caveVerticalScale != 0f)
        {
            float yNormalized = Mathf.InverseLerp(minY, caveStartY, y);
            float scale = Mathf.Lerp(1f, 0.25f, yNormalized * caveVerticalScale);
            noise *= scale;
        }

        if (caveHorizontalScale != 0f)
        {
            float xNormalized = Mathf.InverseLerp(minX, maxX, x);
            float scale = Mathf.Lerp(1f, 0.25f, xNormalized * caveHorizontalScale);
            noise *= scale;
        }

        return noise;
    }

    /// <summary>
    /// Spawn or delete cave asset at a grid position depending on debug and z-layer.
    /// </summary>
    public void HandleCaveAsset(Vector3Int gridPosition)
    {
        if (visibleCaveTileAsset == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave tile asset assigned in Inspector.");
            return;
        }
        if (targetTilemap == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No target Tilemap assigned in Inspector.");
            return;
        }
        // Only handle tiles on the correct z-layer
        if (gridPosition.z != targetZLayer)
        {
            // If not on the correct z, always delete any cave tile here
            targetTilemap.SetTile(gridPosition, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.up, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.down, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.left, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.right, null);
            return;
        }
        if (!debug)
        {
            // Debug off: delete cave tile
            targetTilemap.SetTile(gridPosition, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.up, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.down, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.left, null);
            targetTilemap.SetTile(gridPosition + Vector3Int.right, null);
            return;
        }
        // Debug on and z matches: spawn cave tile
        targetTilemap.SetTile(gridPosition, visibleCaveTileAsset);
        targetTilemap.SetTile(gridPosition + Vector3Int.up, visibleCaveTileAsset);
        targetTilemap.SetTile(gridPosition + Vector3Int.down, visibleCaveTileAsset);
        targetTilemap.SetTile(gridPosition + Vector3Int.left, visibleCaveTileAsset);
        targetTilemap.SetTile(gridPosition + Vector3Int.right, visibleCaveTileAsset);
    }

    /// <summary>
    /// Draw cave tiles from archive, with debug/z-layer logic.
    /// </summary>
    public void DrawCavesFromArchive(ChunkedWorldArchive archive)
    {
        if (archive == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No archive provided.");
            return;
        }
        if (visibleCaveTileAsset == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave tile asset assigned in Inspector.");
            return;
        }
        if (targetTilemap == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No target Tilemap assigned in Inspector.");
            return;
        }

        var allTiles = archive.AllTiles();

        foreach (var pair in allTiles)
        {
            Vector3Int gridPos = pair.Key;
            TileData tileData = pair.Value;

            if (tileData != null && tileData.blockTagOrName == "cave")
            {
                HandleCaveAsset(gridPos);
            }
            else
            {
                // If not a cave, make sure to clear any cave asset here
                if (gridPos.z == targetZLayer)
                {
                    targetTilemap.SetTile(gridPos, null);
                    targetTilemap.SetTile(gridPos + Vector3Int.up, null);
                    targetTilemap.SetTile(gridPos + Vector3Int.down, null);
                    targetTilemap.SetTile(gridPos + Vector3Int.left, null);
                    targetTilemap.SetTile(gridPos + Vector3Int.right, null);
                }
            }
        }
    }

    void Update()
    {
        if (playerTransform != null)
        {
            DrawCavesFromArchive(new ChunkedWorldArchive("default"));
            // Optionally test at origin:
            HandleCaveAsset(new Vector3Int(0, 0, targetZLayer));
        }
    }
}