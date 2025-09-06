using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Modular infinite world spawner.
/// ALL tilemap layers are independent. If a tile is deleted in the archive for a layer/z,
/// it will NEVER be respawned for that layer/z, but other layers and z's are unaffected.
/// </summary>
public class InfiniteCameraSpawnerModular : MonoBehaviour
{
    // --- Terrain/Seed Parameters ---
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
    public float canyonThreshold = 0.9f; // <-- Added missing field!
    public AnimationCurve randomHillCurve;

    // --- Tilemap Z Spacing (for multi-layer worlds) ---
    [Serializable]
    public class TilemapZSpacing
    {
        public Tilemap tilemap;
        public float zSpacing = 1f;
    }
    [Header("Tilemap Z Spacings")]
    public List<TilemapZSpacing> tilemapZSpacings = new List<TilemapZSpacing>();

    // --- Archive Manager ---
    [Header("World Archive")]
    public WorldArchiveManager worldArchiveManager;
    public bool enableWorldArchive = true;

    // --- Tilemaps ---
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap frontTilemap;
    public Tilemap middleFrontTilemap;
    public Tilemap middleBackTilemap;
    public Tilemap backTilemap;

    // --- Tiles ---
    [Header("Tiles")]
    public TileBase defaultSurfaceTile;
    public TileBase defaultSubsurfaceTile;
    public TileBase defaultGroundTile;
    public TileBase defaultBedrockTile;
    public TileBase defaultAirTile; // Air tile for all layers

    // --- Player ---
    public Transform playerTransform;

    // --- Biome Manager Fallback (optional) ---
    [Header("Biome Manager Fallback (optional)")]
    public BiomeManagerFallback biomeManagerFallback; // Optional

    [Header("Biome Selection")]
    public string selectedBiomeName = "";
    public int selectedBiomeIndex = 0;

    // --- Area Size & Depth ---
    [Header("Area Size & Depth")]
    public float visualAreaSize = 30f;
    public int tilesBelow = 50;
    public int worldBottomY = -100;

    // --- Chunk/World Logic ---
    [Header("Chunk Controls (optional)")]
    public int chunkSize = 16;
    public int chunksSpawnDistance = 10;
    public ModularChunkManager chunkManager; // Optional

    // --- Archiving (optional) ---
    [Header("Archiving (optional)")]
    public WorldArchiveManager worldArchiveMa; // Optional

    // --- Seed Modular (optional) ---
    [Header("Seed Modular (optional)")]
    public SeedSelector seedSelector; // Optional

    // --- Optional per-layer cave utilities (for true independence!) ---
    [Header("Cave Generators (modular, optional per layer)")]
    public ModularCaveUtility groundCaveUtility;
    public ModularCaveUtility frontCaveUtility;
    public ModularCaveUtility middleFrontCaveUtility;
    public ModularCaveUtility middleBackCaveUtility;
    public ModularCaveUtility backCaveUtility;

    // --- Optional per-layer seeds for independence ---
    [Header("Layer Seeds (for independence)")]
    public int groundSeed = 101;
    public int frontSeed = 202;
    public int middleFrontSeed = 303;
    public int middleBackSeed = 404;
    public int backSeed = 505;

    public float groundZOffset = 0f;
    public float frontZOffset = 1f;
    public float middleFrontZOffset = 2f;
    public float middleBackZOffset = -1f;
    public float backZOffset = -2f;

    void Awake()
    {
        ApplySeedIfPresent();
    }

    public void ApplySeedIfPresent()
    {
        if (seedSelector != null)
        {
            int hash = seedSelector.usedSeedInt;
            System.Random rand = new System.Random(hash);

            seedScale = SeededValue(rand, 0.03f, 0.09f, 1);
            hillHeight = SeededValue(rand, 5f, 24f, 2);
            hillNoiseScale = SeededValue(rand, 0.03f, 0.12f, 3);
            cliffSharpness = SeededValue(rand, 1.2f, 3.0f, 4);
            hillVerticalShift = SeededValue(rand, -8f, 4f, 5);
            canyonThreshold = SeededValue(rand, 0.8f, 0.97f, 6);
            seedAmplitude = SeededValue(rand, 0.6f, 3.0f, 7);
            hillCurveRandomJitter = SeededValue(rand, 0.08f, 0.7f, 8);
            hillRandomAmplitude = SeededValue(rand, 0.05f, 0.9f, 9);
            curveShift = SeededValue(rand, -1.2f, 1.2f, 10);
            perlinOffsetX = SeededValue(rand, 0f, 100f, 11);
            perlinOffsetZ = SeededValue(rand, 0f, 100f, 12);
            perlinStrength = SeededValue(rand, 0.3f, 1.6f, 13);
            perlinBase = SeededValue(rand, 0f, 1.0f, 14);
        }
    }

    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }

    void OnApplicationQuit()
    {
        if (worldArchiveMa != null)
        {
            worldArchiveMa.SaveAll();
            Debug.Log("WorldArchiveManager: saved on exit.");
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

    /// <summary>
    /// Returns the surface Y at (x, z) using current seed and terrain parameters.
    /// </summary>
    public int GetSurfaceY(int x, int z)
    {
        float layerOffset = Mathf.PerlinNoise(z * hillNoiseScale + perlinOffsetZ, curveShift * 0.29f) * 2f - 1f;
        float t = x * seedScale + curveShift + z * seedScale * 0.11f + layerOffset * hillRandomAmplitude * 2f;
        float tCurve = (t * 0.001f) + 0.5f;
        tCurve = Mathf.Clamp01(tCurve);
        float curveValue = randomHillCurve != null ? randomHillCurve.Evaluate(tCurve) * seedAmplitude : Mathf.Sin(t) * seedAmplitude;

        float noiseValue = Mathf.PerlinNoise(x * hillNoiseScale + perlinOffsetX, z * hillNoiseScale + perlinOffsetZ);
        float extraNoise = Mathf.PerlinNoise(x * hillNoiseScale * 0.2f + perlinBase, z * hillNoiseScale * 0.2f + perlinBase) - 0.5f;
        noiseValue = (noiseValue - 0.5f) * perlinStrength + extraNoise * (hillRandomAmplitude * 0.7f);
        noiseValue = Mathf.Sign(noiseValue) * Mathf.Pow(Mathf.Abs(noiseValue), cliffSharpness);

        float canyonMask = Mathf.PerlinNoise(z * 0.12f + perlinOffsetZ * 0.5f, x * 0.015f + perlinOffsetX * 0.25f);
        if (canyonMask > 0.92f)
        {
            return Mathf.RoundToInt(hillHeight * 2f);
        }
        else if (canyonMask < 0.08f)
        {
            return Mathf.RoundToInt(-hillHeight * 2f);
        }

        float finalValue = curveValue + (noiseValue * hillRandomAmplitude) + hillVerticalShift;
        return Mathf.RoundToInt(finalValue * hillHeight);
    }

    /// <summary>
    /// Checks if a Vector3Int position is in player's Z safe range (+-2).
    /// </summary>
    public bool IsInPlayerZSafeRange(Vector3Int pos)
    {
        if (playerTransform == null) return false;
        int playerZ = Mathf.RoundToInt(playerTransform.position.z);
        return pos.z >= playerZ - 2 && pos.z <= playerZ + 2;
    }

    /// <summary>
    /// Example stub for chunk spawning. Extend with your chunk generation logic.
    /// </summary>
    public void SpawnOrLoadChunk_Defensive(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render)
    {
        // Implement your chunk logic here.
        Debug.Log($"SpawnOrLoadChunk_Defensive called: chunkX={chunkX}, z={z}, buildBottom={buildBottom}, maxY={maxY}, playerZ={playerZ}, render={render}");
    }

    /// <summary>
    /// Generate terrain for all layers, each layer is fully independent.
    /// </summary>
    public void UpdateWorldIfNeeded()
    {
        if (chunkManager != null)
        {
            return;
        }
        if (groundTilemap == null || playerTransform == null)
            return;

        Vector3Int playerCell = groundTilemap.WorldToCell(playerTransform.position);
        int numCellsX = Mathf.RoundToInt(visualAreaSize / Mathf.Abs(groundTilemap.layoutGrid.cellSize.x));
        int halfCellsX = numCellsX / 2;
        int minX = playerCell.x - halfCellsX;
        int maxX = minX + numCellsX - 1;
        int minY = Mathf.Max(playerCell.y - tilesBelow + 1, worldBottomY);
        int maxY = playerCell.y;

        // Each layer uses its own cave utility!
        GenerateLayer(groundTilemap, groundZOffset, groundSeed, groundCaveUtility, minX, maxX, minY, maxY, playerCell.z, "ground");
        GenerateLayer(frontTilemap, frontZOffset, frontSeed, frontCaveUtility, minX, maxX, minY, maxY, playerCell.z, "front");
        GenerateLayer(middleFrontTilemap, middleFrontZOffset, middleFrontSeed, middleFrontCaveUtility, minX, maxX, minY, maxY, playerCell.z, "middleFront");
        GenerateLayer(middleBackTilemap, middleBackZOffset, middleBackSeed, middleBackCaveUtility, minX, maxX, minY, maxY, playerCell.z, "middleBack");
        GenerateLayer(backTilemap, backZOffset, backSeed, backCaveUtility, minX, maxX, minY, maxY, playerCell.z, "back");
    }

    /// <summary>
    /// Generate terrain for one layer. This logic is 100% independent per layer!
    /// If a tile is deleted in the archive for this layer/z, it will NEVER be set again.
    /// </summary>
    void GenerateLayer(
        Tilemap tilemap,
        float zOffset,
        int layerSeed,
        ModularCaveUtility caveUtility,
        int minX, int maxX, int minY, int maxY, int playerZ,
        string layerName
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

        for (int x = minX; x <= maxX; x++)
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

            // --- ARCHIVE DELETION CHECK for THIS layer/z ---
            if (worldArchiveMa != null && worldArchiveMa.IsTileDeleted(surfacePos))
            {
                tilemap.SetTile(surfacePos, null);
                continue; // skip this tile - NEVER respawn if deleted
            }

            // CAVE LOGIC: uses caveUtility for THIS layer!
            bool isCave = caveUtility != null && caveUtility.IsCave(x, surfaceY, z, surfaceY);
            TileBase surfaceTile = isCave ? defaultAirTile : GetTileForType(biome, "surface");
            tilemap.SetTile(surfacePos, surfaceTile);
            if (worldArchiveMa != null) worldArchiveMa.ArchiveTile(surfacePos, surfaceTile != null ? surfaceTile.name : "air");

            // Above surface is always air for THIS layer.
            Vector3Int airPos = new Vector3Int(x, surfaceY + 1, z);
            if (worldArchiveMa != null && worldArchiveMa.IsTileDeleted(airPos))
            {
                tilemap.SetTile(airPos, null);
            }
            else
            {
                tilemap.SetTile(airPos, defaultAirTile);
                if (worldArchiveMa != null) worldArchiveMa.ArchiveTile(airPos, "air");
            }

            // Fill below surface for THIS layer (independent cave/ground/bedrock).
            for (int y = surfaceY - 1; y >= minY; y--)
            {
                Vector3Int belowPos = new Vector3Int(x, y, z);
                if (worldArchiveMa != null && worldArchiveMa.IsTileDeleted(belowPos))
                {
                    tilemap.SetTile(belowPos, null);
                    continue; // skip this tile - NEVER respawn if deleted
                }

                bool isCaveBelow = caveUtility != null && caveUtility.IsCave(x, y, z, surfaceY);
                TileBase groundTile = isCaveBelow ? defaultAirTile
                    : (y == minY ? GetTileForType(biome, "bedrock") : (y > minY + 2 ? GetTileForType(biome, "ground") : GetTileForType(biome, "subsurface")));
                tilemap.SetTile(belowPos, groundTile);
                if (worldArchiveMa != null) worldArchiveMa.ArchiveTile(belowPos, groundTile != null ? groundTile.name : "air");
            }
        }
    }

    public void DeleteTile(Vector3Int pos)
    {
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);

        if (worldArchiveMa != null)
        {
            worldArchiveMa.DeleteTile(pos, groundTilemap);
        }
    }
}