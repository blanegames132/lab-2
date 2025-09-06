using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Modular infinite world spawner.
/// If ModularChunkManager is active, spawns only requested chunks (with throttling), otherwise uses internal logic.
/// Never knows about archive manager.
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

    [Serializable]
    public class TilemapZSpacing
    {
        public Tilemap tilemap;
        public float zSpacing = 1f;
    }

    [Header("Chunk Controls")]
    public int chunkSize = 16;
    public bool enableChunkManager = true;
    public ModularChunkManager chunkManager;

    [Header("Tilemaps and Terrain")]
    public List<TilemapZSpacing> tilemapZSpacings = new List<TilemapZSpacing>();
    public Tilemap groundTilemap, frontTilemap, middleFrontTilemap, middleBackTilemap, backTilemap;
    public TileBase defaultSurfaceTile, defaultSubsurfaceTile, defaultGroundTile, defaultBedrockTile, defaultAirTile;
    public Transform playerTransform;

    [Header("Biome/Generation")]
    public BiomeManagerFallback biomeManagerFallback;
    public string selectedBiomeName = "";
    public int selectedBiomeIndex = 0;
    public float visualAreaSize = 30f;
    public int tilesBelow = 50, worldBottomY = -100;
    public float groundZOffset = 0f, frontZOffset = 1f, middleFrontZOffset = 2f, middleBackZOffset = -1f, backZOffset = -2f;
    public ModularCaveUtility groundCaveUtility, frontCaveUtility, middleFrontCaveUtility, middleBackCaveUtility, backCaveUtility;
    public int groundSeed = 101, frontSeed = 202, middleFrontSeed = 303, middleBackSeed = 404, backSeed = 505;

    // Called by chunk manager: request to spawn a chunk (throttled)
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
        int centerZ = playerTransform != null ? Mathf.RoundToInt(playerTransform.position.z) : 0;

        int steps = isFarChunk ? 100 : 1; // 100x slower for far chunks

        for (int step = 0; step < steps; step++)
        {
            GenerateLayer(groundTilemap, groundZOffset, groundSeed, groundCaveUtility, minX, maxX, minY, maxY, centerZ, "ground", step, steps);
            GenerateLayer(frontTilemap, frontZOffset, frontSeed, frontCaveUtility, minX, maxX, minY, maxY, centerZ, "front", step, steps);
            GenerateLayer(middleFrontTilemap, middleFrontZOffset, middleFrontSeed, middleFrontCaveUtility, minX, maxX, minY, maxY, centerZ, "middleFront", step, steps);
            GenerateLayer(middleBackTilemap, middleBackZOffset, middleBackSeed, middleBackCaveUtility, minX, maxX, minY, maxY, centerZ, "middleBack", step, steps);
            GenerateLayer(backTilemap, backZOffset, backSeed, backCaveUtility, minX, maxX, minY, maxY, centerZ, "back", step, steps);
            yield return null;
        }
    }

    // Internal auto mode: draws all tiles in area if no chunk manager
    void Update()
    {
        if (!enableChunkManager || chunkManager == null)
        {
            UpdateWorldIfNeeded();
        }
        // else: chunk manager drives everything via RequestChunkSpawn
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

        GenerateLayer(groundTilemap, groundZOffset, groundSeed, groundCaveUtility, minX, maxX, minY, maxY, playerCell.z, "ground", 0, 1);
        GenerateLayer(frontTilemap, frontZOffset, frontSeed, frontCaveUtility, minX, maxX, minY, maxY, playerCell.z, "front", 0, 1);
        GenerateLayer(middleFrontTilemap, middleFrontZOffset, middleFrontSeed, middleFrontCaveUtility, minX, maxX, minY, maxY, playerCell.z, "middleFront", 0, 1);
        GenerateLayer(middleBackTilemap, middleBackZOffset, middleBackSeed, middleBackCaveUtility, minX, maxX, minY, maxY, playerCell.z, "middleBack", 0, 1);
        GenerateLayer(backTilemap, backZOffset, backSeed, backCaveUtility, minX, maxX, minY, maxY, playerCell.z, "back", 0, 1);
    }

    void GenerateLayer(
        Tilemap tilemap,
        float zOffset,
        int layerSeed,
        ModularCaveUtility caveUtility,
        int minX, int maxX, int minY, int maxY, int playerZ,
        string layerName,
        int step, int steps
    )
    {
        if (tilemap == null) return;
        int z = playerZ + Mathf.RoundToInt(zOffset);
        System.Random rand = new System.Random(layerSeed + z * 999);
        float layerNoiseScale = hillNoiseScale * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerCliffSharpness = cliffSharpness * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerHillHeight = hillHeight * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerVerticalShift = hillVerticalShift * (1f + (rand.Next(-10, 10) * 0.01f));
        float layerCanyonThreshold = canyonThreshold * (1f + (rand.Next(-10, 10) * 0.01f));
        Biome biome = GetActiveBiome();

        // Throttled drawing: each step only draws a fraction of chunk
        int totalX = maxX - minX + 1;
        int beginX = minX + step * totalX / steps;
        int endX = minX + (step + 1) * totalX / steps - 1;
        endX = Mathf.Min(endX, maxX);

        for (int x = beginX; x <= endX; x++)
        {
            float hill = Mathf.PerlinNoise((x + layerSeed) * layerNoiseScale, (z + layerSeed) * layerNoiseScale);
            hill = Mathf.Pow(hill, layerCliffSharpness);
            float canyonMask = Mathf.PerlinNoise((z + layerSeed) * 0.12f, (x + layerSeed) * 0.015f);

            float height = hill * layerHillHeight + layerVerticalShift;
            if (canyonMask > layerCanyonThreshold)
                height += layerHillHeight * 2f;
            else if (canyonMask < 1f - layerCanyonThreshold)
                height -= layerHillHeight * 2f;

            int surfaceY = Mathf.RoundToInt(height);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);

            // --- ARCHIVE DELETION CHECK: via bus ---
            if (TileEventBus.QueryShouldBlockTile(surfacePos))
            {
                tilemap.SetTile(surfacePos, null);
                continue;
            }

            bool isCave = caveUtility != null && caveUtility.IsCave(x, surfaceY, z, surfaceY);
            TileBase surfaceTile = isCave ? defaultAirTile : GetTileForType(biome, "surface");
            tilemap.SetTile(surfacePos, surfaceTile);
            TileEventBus.BroadcastTileSet(tilemap, surfacePos, surfaceTile);

            Vector3Int airPos = new Vector3Int(x, surfaceY + 1, z);
            if (TileEventBus.QueryShouldBlockTile(airPos))
            {
                tilemap.SetTile(airPos, null);
            }
            else
            {
                tilemap.SetTile(airPos, defaultAirTile);
                TileEventBus.BroadcastTileSet(tilemap, airPos, defaultAirTile);
            }

            for (int y = surfaceY - 1; y >= minY; y--)
            {
                Vector3Int belowPos = new Vector3Int(x, y, z);
                if (TileEventBus.QueryShouldBlockTile(belowPos))
                {
                    tilemap.SetTile(belowPos, null);
                    continue;
                }

                bool isCaveBelow = caveUtility != null && caveUtility.IsCave(x, y, z, surfaceY);
                TileBase groundTile = isCaveBelow ? defaultAirTile
                    : (y == minY ? GetTileForType(biome, "bedrock") : (y > minY + 2 ? GetTileForType(biome, "ground") : GetTileForType(biome, "subsurface")));
                tilemap.SetTile(belowPos, groundTile);
                TileEventBus.BroadcastTileSet(tilemap, belowPos, groundTile);
            }
        }
    }

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
        if (groundTilemap != null)
        {
            TileBase t = groundTilemap.GetTile(cell);
            if (t != null) return t;
        }
        if (frontTilemap != null)
        {
            TileBase t = frontTilemap.GetTile(cell);
            if (t != null) return t;
        }
        if (middleFrontTilemap != null)
        {
            TileBase t = middleFrontTilemap.GetTile(cell);
            if (t != null) return t;
        }
        if (middleBackTilemap != null)
        {
            TileBase t = middleBackTilemap.GetTile(cell);
            if (t != null) return t;
        }
        if (backTilemap != null)
        {
            TileBase t = backTilemap.GetTile(cell);
            if (t != null) return t;
        }
        return null;
    }
}