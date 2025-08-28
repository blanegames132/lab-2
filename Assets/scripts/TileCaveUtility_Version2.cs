using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCaveUtility : MonoBehaviour
{
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

    public Transform playerTransform;
    public bool IsInitialized { get; private set; } = false;

    public int minX = -100;
    public int maxX = 100;
    public int minY = -120;
    public int maxY = 30;
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
    public void DiscoverAndDeleteCavesInRadius(Vector3 center, float radius, ChunkedWorldArchive archive)
    {
        var toDelete = new List<Vector3Int>();
        foreach (var pair in archive.AllTiles())
        {
            Vector3Int pos = pair.Key;
            TileData data = pair.Value;
            if (Vector3.Distance(center, pos) <= radius && data != null && data.blockTagOrName == "cave")
            {
                toDelete.Add(pos);
            }
        }
        foreach (var pos in toDelete)
        {
            archive.RemoveTile(pos);
            Debug.Log($"Cave tile deleted from archive at {pos}");
        }
        if (toDelete.Count > 0)
            archive.SaveAll();

        // Clear tiles in both tilemaps, all z
        void ClearAllTilesInTilemap(Tilemap tilemap)
        {
            if (tilemap == null) return;
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (Vector3.Distance(center, pos) <= radius)
                {
                    if (tilemap.GetTile(pos) != null)
                    {
                        tilemap.SetTile(pos, null);
                        Debug.Log($"Tile cleared from tilemap at {pos}");
                    }
                }
            }
        }
        ClearAllTilesInTilemap(caveTilemap);
        ClearAllTilesInTilemap(caveTilemapFog);
    }

    /// <summary>
    /// Draw caves: debug on caveTilemap, fog on caveTilemapFog, all z
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
        if (caveTilemap == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave Tilemap assigned in Inspector.");
            return;
        }
        if (caveTilemapFog == null)
        {
            UnityEngine.Debug.LogError("TileCaveUtility: No cave TilemapFog assigned in Inspector.");
            return;
        }

        var allTiles = archive.AllTiles();

        foreach (var pair in allTiles)
        {
            Vector3Int gridPos = pair.Key;
            TileData tileData = pair.Value;

            if (tileData != null && tileData.blockTagOrName == "cave")
            {
                // Normal debug: always draw if debug is on
                if (debug)
                {
                    HandleCaveAsset(gridPos);
                }

                // FOG DEBUG: draw ONLY if NOT discovered, else remove from fog tilemap
                if (debugFog)
                {
                    if (!tileData.discovered)
                    {
                        HandleCaveAssetFog(gridPos); // show tile in fog
                    }
                    else
                    {
                        caveTilemapFog.SetTile(gridPos, null); // remove tile from fog if discovered
                    }
                }

                // Log discovered tiles
                if (tileData.discovered)
                {
                    Debug.Log($"Tile at {gridPos} is discovered.");
                }
            }
            else
            {
                caveTilemap.SetTile(gridPos, null);
                caveTilemapFog.SetTile(gridPos, null);
            }
        }
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
            tileCaveUtility.DiscoverAndDeleteCavesInRadius(playerGridPos, discoveryRadius, archive);
        }
    }
}