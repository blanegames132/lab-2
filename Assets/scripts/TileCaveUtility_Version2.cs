using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCaveUtility : MonoBehaviour
{
    public TileInfiniteCameraSpawner spawner;
    private Vector3Int lastTriggeredPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.09f;
    public float caveThreshold = 0.5f;
    public float caveHorizontalMultiplier = 1.0f;
    public float caveVerticalMultiplier = 1.0f;
    [Range(0.5f, 5f)]
    public float caveSharpness = 1.0f;
    public float secondaryFrequency = 0.0f;
    [Range(0f, 1f)]
    public float secondaryWeight = 0.0f;
    [Range(-1f, 1f)]
    public float verticalBias = 0.0f;
    [Range(0f, 1f)]
    public float verticalBiasStrength = 0.0f;

    [Header("Cave Shape/Curve Controls")]
    public float caveCurve = 0.0f;
    public float caveVerticalScale = 0.0f;
    public float caveHorizontalScale = 0.0f;

    [Header("Deep Cave Controls")]
    public int surfaceDepth = 100;

    [Header("Cave Entrance Controls")]
    [Range(0f, 1f)]
    public float caveEntranceChance = 0.3f;
    [Range(0f, 2f)]
    public float caveEntranceZigzag = 1.0f;
    [Range(2, 8)]
    public int caveEntranceWidth = 2;

    public int caveEntranceAbove = 10;
    public int caveEntranceBelow = 20;
    public int caveExitYOffset = 50;
    public int caveExitXOffset = 60;

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;
    public TileBase visibleCaveTileAsset2;

    [Tooltip("Tilemap to spawn cave assets into. Assign in Inspector.")]
    public Tilemap caveTilemap;

    [Tooltip("Tilemap to spawn cave assets into (Fog Debug). Assign in Inspector.")]
    public Tilemap caveTilemapFog;

    [Header("Hidden/Backup Tilemap")]
    [Tooltip("Tilemap to spawn hidden cave tiles into if below threshold. Assign in Inspector.")]
    public Tilemap caveTilemapHidden;

    public Transform playerTransform;
    public bool IsInitialized { get; private set; } = false;

    public int minX = int.MinValue;
    public int maxX = int.MaxValue;
    public int minY = int.MinValue;
    public int maxY = int.MaxValue;
    public int z = 0;

    [Header("Debug Controls (Normal)")]
    public bool debug = true;
    public int targetZLayer = 0;

    [Header("Debug Controls (Fog)")]
    public bool debugFog = true;
    public int targetZLayerFog = 0;
    void Awake()
    {
        IsInitialized = true;
    }

    public bool IsCaveAt(int x, int y, int z, int surfaceY)
    {
        for (int dx = -caveEntranceWidth / 2; dx <= caveEntranceWidth / 2; dx++)
        {
            if (IsCaveEntranceSingle(x + dx, y, z, surfaceY) &&
                (IsCaveEntranceSingle(x + dx, y + 1, z, surfaceY) || IsCaveEntranceSingle(x + dx, y - 1, z, surfaceY)))
            {
                return true;
            }
        }
        return CaveGenerator(x, y, z, surfaceY) > caveThreshold;
    }

    public bool IsCaveEntranceSingle(int x, int y, int z, int surfaceY)
    {
        int entranceTop = surfaceY + caveEntranceAbove;
        int entranceBottom = surfaceY - caveEntranceBelow;
        if (y > entranceTop || y < entranceBottom - caveExitYOffset) return false;

        int entranceSeed = x * 73856093 ^ z * 19349663;
        System.Random entranceRand = new System.Random(entranceSeed);

        if (entranceRand.NextDouble() >= caveEntranceChance)
            return false;

        bool exitLeft = entranceRand.NextDouble() < 0.5;
        int exitY = entranceBottom - caveExitYOffset;
        int exitX = x + (exitLeft ? -caveExitXOffset : caveExitXOffset);

        float t = Mathf.InverseLerp(entranceTop, exitY, y);
        float curvePower = 1.7f;
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

    // ---------------- DEBUG: operate only on caveTilemap, all z ----------------
    public void HandleCaveAsset(Vector3Int gridPosition)
    {
        if (visibleCaveTileAsset2 == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave tile asset 2 assigned in Inspector.");
            return;
        }
        if (caveTilemap == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave Tilemap assigned in Inspector.");
            return;
        }
        if (debug)
        {
            caveTilemap.SetTile(gridPosition, visibleCaveTileAsset2);
        }
        else
        {
            caveTilemap.SetTile(gridPosition, null);
        }
    }

    // ---------------- FOG: operate only on caveTilemapFog, all z ----------------
    public void HandleCaveAssetFog(Vector3Int gridPosition)
    {
        if (visibleCaveTileAsset == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave tile asset assigned in Inspector.");
            return;
        }
        if (caveTilemapFog == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave TilemapFog assigned in Inspector.");
            return;
        }
        if (debugFog)
        {
            caveTilemapFog.SetTile(gridPosition, visibleCaveTileAsset);
        }
        else
        {
            caveTilemapFog.SetTile(gridPosition, null);
        }
    }

    /// <summary>
    /// Mark all cave tiles within radius as discovered, remove fog, and persist to archive.
    /// </summary>

public void DrawCavesFromArchive(
        ChunkedWorldArchive archive,
        int playerZ,
        float threshold = 0.5f)
    {
        if (archive == null || caveTilemap == null || caveTilemapFog == null || caveTilemapHidden == null)
            return;

        // Clear all 3 tilemaps for relevant z layers
        foreach (var pos in caveTilemap.cellBounds.allPositionsWithin)
            if (pos.z == playerZ)
                caveTilemap.SetTile(pos, null);
        foreach (var pos in caveTilemapFog.cellBounds.allPositionsWithin)
            if (pos.z == playerZ + 1)
                caveTilemapFog.SetTile(pos, null);
        foreach (var pos in caveTilemapHidden.cellBounds.allPositionsWithin)
            if (pos.z == playerZ - 1)
                caveTilemapHidden.SetTile(pos, null);

        var spawner = GetComponent<TileInfiniteCameraSpawner>();
        float hillHeight = spawner != null ? spawner.hillHeight : 1f;

        foreach (var pair in archive.AllTiles())
        {
            Vector3Int gridPos = pair.Key;
            TileData tileData = pair.Value;
            if (tileData == null || tileData.blockTagOrName != "cave") continue;

            // Defensive: Only spawn UNDISCOVERED caves!
            if (tileData.discovered)
            {
                // Remove from all 3 layers if it's discovered
                caveTilemap.SetTile(gridPos, null);
                caveTilemapFog.SetTile(gridPos, null);
                caveTilemapHidden.SetTile(gridPos, null);
                continue;
            }

            int surfaceY = 0;
            if (spawner != null)
                surfaceY = Mathf.RoundToInt(spawner.GetHillValue(gridPos.x, gridPos.z) * hillHeight);

            float val = CaveGenerator(gridPos.x, gridPos.y, gridPos.z, surfaceY);

            // Player's current z: visible cave
            if (gridPos.z == playerZ)
            {
                caveTilemap.SetTile(gridPos, val >= threshold ? visibleCaveTileAsset2 : null);
                caveTilemapFog.SetTile(gridPos, null);
                caveTilemapHidden.SetTile(gridPos, null);
            }
            // Player's z+1: fog/hidden above
            else if (gridPos.z == playerZ + 1)
            {
                caveTilemap.SetTile(gridPos, null);
                caveTilemapFog.SetTile(gridPos, val >= threshold ? visibleCaveTileAsset : null);
                caveTilemapHidden.SetTile(gridPos, null);
            }
            // Player's z-1: fog/hidden below
            else if (gridPos.z == playerZ - 1)
            {
                caveTilemap.SetTile(gridPos, null);
                caveTilemapFog.SetTile(gridPos, null);
                caveTilemapHidden.SetTile(gridPos, val >= threshold ? visibleCaveTileAsset : null);
            }
            // Any other z: clear all
            else
            {
                caveTilemap.SetTile(gridPos, null);
                caveTilemapFog.SetTile(gridPos, null);
                caveTilemapHidden.SetTile(gridPos, null);
            }
        }
    }

    public void EnsureTileOnCorrectTilemap(Vector3Int gridPos, int playerZ)
    {
        if (caveTilemap == null || caveTilemapFog == null || caveTilemapHidden == null)
            return;

        // Remove from all tilemaps first
        caveTilemap.SetTile(gridPos, null);
        caveTilemapFog.SetTile(gridPos, null);
        caveTilemapHidden.SetTile(gridPos, null);

        // Place only on the correct tilemap based on z
        if (gridPos.z == playerZ)
        {
            caveTilemap.SetTile(gridPos, visibleCaveTileAsset2);
        }
        else if (gridPos.z == playerZ + 1)
        {
            caveTilemapFog.SetTile(gridPos, visibleCaveTileAsset);
        }
        else if (gridPos.z == playerZ - 1)
        {
            caveTilemapHidden.SetTile(gridPos, visibleCaveTileAsset);
        }
        // Else, do not place anywhere
    }
    public void DiscoverAndMarkCavesInRadius(Vector3 center, float radius, ChunkedWorldArchive archive)
    {
        if (archive == null) return;

        bool changed = false;
        foreach (var pair in archive.AllTiles())
        {
            Vector3Int pos = pair.Key;
            TileData data = pair.Value;
            if (Vector3.Distance(center, pos) <= radius && data != null && data.blockTagOrName == "cave" && !data.discovered)
            {
                data.discovered = true;
                archive.SetTile(pos, data);
                changed = true;
            }
        }
        if (changed)
            archive.SaveAll();
    }

    public class FogOfWarSystem : MonoBehaviour
    {
        public Transform player;
        public Tilemap caveTilemapFog;
        public ChunkedWorldArchive archive;
        public float discoveryRadius = 5f;
        public TileCaveUtility tileCaveUtility;

        void Update()
        {
            if (player == null || caveTilemapFog == null || archive == null || tileCaveUtility == null)
                return;

            Vector3Int playerGridPos = Vector3Int.FloorToInt(player.position);

            // Mark caves as discovered in radius and update fog (removes from archive)
            tileCaveUtility.DiscoverAndMarkCavesInRadius(playerGridPos, discoveryRadius, archive);
        }
    }
}