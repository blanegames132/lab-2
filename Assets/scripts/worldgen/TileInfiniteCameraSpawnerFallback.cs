using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Modular infinite world spawner. Handles chunk spawning, tile generation,
/// persistent block deletion, hiding logic, and biome tile selection.
/// </summary>
public class InfiniteCameraSpawnerModular : MonoBehaviour
{
    // --- Terrain/Generation Parameters ---
    [Header("Seed & Terrain")]
    public float seedScale = 0.05f;
    public float seedAmplitude = 1f;
    public float hillHeight = 12f;
    public float hillCurveRandomJitter = 0.1f;
    public float hillRandomAmplitude = 0.2f;
    public float hillNoiseScale = 0.08f;
    public float curveShift = 0f;
    public float perlinOffsetX = 0f;
    public float perlinOffsetZ = 0f;
    public float perlinStrength = 1f;
    public float perlinBase = 0f;
    public float hillVerticalShift = 0f;
    public float cliffSharpness = 2.0f;
    public float canyonThreshold = 0.9f;
    public AnimationCurve randomHillCurve;

    [Header("Tilemaps and Terrain")]
    public Tilemap groundTilemap, frontTilemap, middleFrontTilemap, middleBackTilemap, backTilemap;
    public TileBase defaultSurfaceTile, defaultSubsurfaceTile, defaultGroundTile, defaultBedrockTile, defaultAirTile;
    public Transform playerTransform;

    [Header("Chunk Controls")]
    public int chunkSize = 16;
    public bool enableChunkManager = true;
    public ModularChunkManager chunkManager;

    [Header("Hiding Logic")]
    public TileMultiMapHiddenSet hiddenSetScript; // <-- Assign in Inspector or via code

    [Header("Biome/Generation")]
    public BiomeManagerFallback biomeManagerFallback;
    public string selectedBiomeName = "";
    public int selectedBiomeIndex = 0;
    public float visualAreaSize = 100f;
    public int tilesBelow = 50, worldBottomY = -100;
    public ModularCaveUtility groundCaveUtility, frontCaveUtility, middleFrontCaveUtility, middleBackCaveUtility, backCaveUtility;
    public int groundSeed = 101, frontSeed = 202, middleFrontSeed = 303, middleBackSeed = 404, backSeed = 505;

    // --- Z Layer Assignment ---
    void Update()
    {
        if (!enableChunkManager || chunkManager == null)
        {
            UpdateWorldIfNeeded();
        }
    }

    /// <summary>
    /// Returns the correct tilemap for a given Z (fixed world Z values).
    /// </summary>
    Tilemap GetTilemapForWorldZ(int z)
    {
        switch (z)
        {
            case 0: return groundTilemap;
            case -2: return frontTilemap;
            case 2: return backTilemap;
            case -1: return middleFrontTilemap;
            case 1: return middleBackTilemap;
            default: return null;
        }
    }

    // --- Chunk spawn entry point ---
    public void RequestChunkSpawn(int chunkX, int chunkY, int chunkSize, bool isFarChunk)
    {
        StartCoroutine(GenerateChunkCoroutine(chunkX, chunkY, chunkSize, isFarChunk));
    }

    private System.Collections.IEnumerator GenerateChunkCoroutine(int chunkX, int chunkY, int chunkSize, bool isFarChunk)
    {
        int minX = chunkX * chunkSize;
        int maxX = minX + chunkSize - 1;
        int minY = chunkY * chunkSize;
        int maxY = minY + chunkSize - 1;

        int steps = isFarChunk ? 100 : 1;

        for (int step = 0; step < steps; step++)
        {
            GenerateLayer(0, groundSeed, groundCaveUtility, minX, maxX, minY, maxY, "ground", step, steps);
            GenerateLayer(-2, frontSeed, frontCaveUtility, minX, maxX, minY, maxY, "front", step, steps);
            GenerateLayer(-1, middleFrontSeed, middleFrontCaveUtility, minX, maxX, minY, maxY, "middleFront", step, steps);
            GenerateLayer(1, middleBackSeed, middleBackCaveUtility, minX, maxX, minY, maxY, "middleBack", step, steps);
            GenerateLayer(2, backSeed, backCaveUtility, minX, maxX, minY, maxY, "back", step, steps);
            yield return null;
        }
    }

    /// <summary>
    /// Draws all tiles in the visible area below/around the player.
    /// Used when chunk manager is NOT active.
    /// </summary>
    public void UpdateWorldIfNeeded()
    {
        if (groundTilemap == null || playerTransform == null)
            return;

        Vector3Int playerCell = groundTilemap.WorldToCell(playerTransform.position);
        int numCellsX = Mathf.RoundToInt(visualAreaSize / Mathf.Abs(groundTilemap.layoutGrid.cellSize.x));
        int halfCellsX = numCellsX / 2;
        int minX = playerCell.x - halfCellsX;
        int maxX = minX + numCellsX - 1;
        int minY = Mathf.Max(playerCell.y - tilesBelow + 1, worldBottomY);
        int maxY = playerCell.y;

        GenerateLayer(0, groundSeed, groundCaveUtility, minX, maxX, minY, maxY, "ground", 0, 1);
        GenerateLayer(-2, frontSeed, frontCaveUtility, minX, maxX, minY, maxY, "front", 0, 1);
        GenerateLayer(-1, middleFrontSeed, middleFrontCaveUtility, minX, maxX, minY, maxY, "middleFront", 0, 1);
        GenerateLayer(1, middleBackSeed, middleBackCaveUtility, minX, maxX, minY, maxY, "middleBack", 0, 1);
        GenerateLayer(2, backSeed, backCaveUtility, minX, maxX, minY, maxY, "back", 0, 1);
    }

    // --- Layer generation for assigned Z ---
    void GenerateLayer(
        int worldZ,
        int layerSeed,
        ModularCaveUtility caveUtility,
        int minX, int maxX, int minY, int maxY,
        string layerName,
        int step, int steps
    )
    {
        Tilemap tilemap = GetTilemapForWorldZ(worldZ);
        if (tilemap == null) return;

        System.Random rand = new System.Random(layerSeed + worldZ * 999);
        float layerNoiseScale = hillNoiseScale * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerCliffSharpness = cliffSharpness * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerHillHeight = hillHeight * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerVerticalShift = hillVerticalShift * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerCanyonThreshold = canyonThreshold * (1f + (rand.Next(-10, 10) * 0.01f));
        Biome biome = GetActiveBiome();

        int totalX = maxX - minX + 1;
        int beginX = minX + step * totalX / steps;
        int endX = minX + (step + 1) * totalX / steps - 1;
        endX = Mathf.Min(endX, maxX);

        for (int x = beginX; x <= endX; x++)
        {
            float hill = Mathf.PerlinNoise((x + layerSeed) * layerNoiseScale, (worldZ + layerSeed) * layerNoiseScale);
            hill = Mathf.Pow(hill, layerCliffSharpness);
            float canyonMask = Mathf.PerlinNoise((worldZ + layerSeed) * 0.12f, (x + layerSeed) * 0.015f);

            float height = hill * layerHillHeight + layerVerticalShift;
            if (canyonMask > layerCanyonThreshold)
                height += layerHillHeight * 2f;
            else if (canyonMask < 1f - layerCanyonThreshold)
                height -= layerHillHeight * 2f;

            int surfaceY = Mathf.RoundToInt(height);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, worldZ);

            // Persistent block deletion logic
            if (TileBlockClickHandler.permanentlyDeletedCells.Contains(surfacePos))
            {
                tilemap.SetTile(surfacePos, null);
                continue;
            }

            bool isCave = caveUtility != null && caveUtility.IsCave(x, surfaceY, worldZ, surfaceY);
            TileBase surfaceTile = isCave ? defaultAirTile : GetTileForType(biome, "surface");
            tilemap.SetTile(surfacePos, surfaceTile);
            TileEventBus.BroadcastTileSet(tilemap, surfacePos, surfaceTile);

            if (hiddenSetScript != null)
                hiddenSetScript.HideTileIfShould(tilemap, surfacePos);

            Vector3Int airPos = new Vector3Int(x, surfaceY + 1, worldZ);

            if (TileBlockClickHandler.permanentlyDeletedCells.Contains(airPos))
            {
                tilemap.SetTile(airPos, null);
            }
            else
            {
                tilemap.SetTile(airPos, defaultAirTile);
                TileEventBus.BroadcastTileSet(tilemap, airPos, defaultAirTile);
                if (hiddenSetScript != null)
                    hiddenSetScript.HideTileIfShould(tilemap, airPos);
            }

            for (int y = surfaceY - 1; y >= minY; y--)
            {
                Vector3Int belowPos = new Vector3Int(x, y, worldZ);

                if (TileBlockClickHandler.permanentlyDeletedCells.Contains(belowPos))
                {
                    tilemap.SetTile(belowPos, null);
                    continue;
                }

                bool isCaveBelow = caveUtility != null && caveUtility.IsCave(x, y, worldZ, surfaceY);
                TileBase groundTile = isCaveBelow ? defaultAirTile
                    : (y == minY ? GetTileForType(biome, "bedrock") : (y > minY + 2 ? GetTileForType(biome, "ground") : GetTileForType(biome, "subsurface")));
                tilemap.SetTile(belowPos, groundTile);
                TileEventBus.BroadcastTileSet(tilemap, belowPos, groundTile);
                if (hiddenSetScript != null)
                    hiddenSetScript.HideTileIfShould(tilemap, belowPos);
            }
        }
    }

    // --- Biome/tile logic ---
    Biome GetActiveBiome()
    {
        if (biomeManagerFallback == null || biomeManagerFallback.biomes == null || biomeManagerFallback.biomes.Count == 0)
            return null;
        return biomeManagerFallback.GetBiome(selectedBiomeName, selectedBiomeIndex);
    }

    TileBase GetTileForType(Biome biome, string type)
    {
        if (biome != null)
        {
            if (type == "surface") return biome.surfaceTile ?? defaultSurfaceTile;
            if (type == "subsurface") return biome.subsurfaceTile ?? defaultSubsurfaceTile;
            if (type == "ground") return biome.groundTile ?? defaultGroundTile;
            if (type == "bedrock") return biome.bedrockTile ?? defaultBedrockTile;
        }
        if (type == "surface") return defaultSurfaceTile;
        if (type == "subsurface") return defaultSubsurfaceTile;
        if (type == "ground") return defaultGroundTile;
        if (type == "bedrock") return defaultBedrockTile;
        return defaultGroundTile;
    }

    public TileBase GetActualTileAssetAtCell(Vector3Int cell)
    {
        Tilemap map = GetTilemapForWorldZ(cell.z);
        if (map != null)
        {
            TileBase t = map.GetTile(cell);
            if (t != null) return t;
        }
        return null;
    }
}