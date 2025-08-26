using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TilemapZSpacing
{
    public Tilemap tilemap;
    public float zSpacing = 1f;
}

[System.Serializable]
public class Biome
{
    public string name;
    public TileBase surfaceTile; // grass, sand, snow...
    public TileBase groundTile;  // dirt, sandstone, frozen dirt...
}

public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [Header("Seed Settings")]
    [SerializeField] public bool enableWorldArchive = true;
    [SerializeField] private bool useCustomSeed = false;
    [SerializeField] public string customSeed = "MyWorldSeed";
    [SerializeField] public string hillRandomSeed = "";
    [SerializeField, Tooltip("The actual integer hash generated from hillRandomSeed. Changing this does nothing.")]
    private int generatedSeedHash;
    [SerializeField, Tooltip("The actual string seed used (random if blank at start).")]
    private string usedSeedString;

    [Header("Hill Shape Controls")]
    [SerializeField] private AnimationCurve hillCurve;
    [SerializeField] public float hillHeight;
    [SerializeField] private float cliffSharpness;
    [SerializeField] private float seedAmplitude;
    [SerializeField] private float seedScale;
    [SerializeField] private float hillNoiseScale;
    [SerializeField] private float hillCurveRandomJitter;
    [SerializeField] private float hillRandomAmplitude;
    [SerializeField] private float hillVerticalShift;

    [Header("Tilemap Setup")]
    [SerializeField] public Tilemap groundTilemap;
    [SerializeField] public Tilemap frontTilemap;
    [SerializeField] public Tilemap middleFrontTilemap;
    [SerializeField] public Tilemap middleBackTilemap;
    [SerializeField] public Tilemap backTilemap;
    [SerializeField] public TileBase groundTileAsset;
    [SerializeField] public TileBase grassTileAsset;
    [SerializeField] public TileBase hideTileAsset;
    [SerializeField] public Transform playerTransform;
    [SerializeField] public int buffer = 2;

    [Header("Tilemap Z Spacing")]
    [Tooltip("Set the z spacing for each tilemap here. The order is ground, front, middleFront, middleBack, back.")]
    [SerializeField]
    public List<TilemapZSpacing> tilemapZSpacings = new List<TilemapZSpacing>
    {
        new TilemapZSpacing(), // ground
        new TilemapZSpacing(), // front
        new TilemapZSpacing(), // middleFront
        new TilemapZSpacing(), // middleBack
        new TilemapZSpacing()  // back
    };

    [Header("Biomes")]
    [SerializeField] private List<Biome> biomes = new List<Biome>();

    [Header("Cave Settings")]
    [SerializeField] private TileCaveUtility caveUtility; // Reference to your cave utility

    [Header("Advanced Seed Controls")]
    [SerializeField] private float repeatRange;
    [SerializeField] private float curveShift;
    [SerializeField] private float perlinOffsetX;
    [SerializeField] private float perlinOffsetZ;
    [SerializeField] private float perlinStrength;
    [SerializeField] private float perlinBase;

    [Header("World Controls")]
    [SerializeField] private int worldBottomY = -100;

    [Header("Chunk Settings")]
    [SerializeField] private int ChunkSize = 16;
    [SerializeField] private int ChunksVisible = 3;
    [SerializeField] private int ChunksGenerated = 6;

    [Header("Hidden Tiles")]
    [SerializeField] private TileHiddenSet tileHiddenSet;
    [SerializeField] private MidFrontTileHiddenSet midFrontTileHiddenSet;

    // --- Optimization: Reuse data structures
    private static readonly List<Vector3Int> toRemove = new List<Vector3Int>();

    // --- NEW: Track last player position so we can do any unload only at quit
    private Vector3Int? lastPlayerPosForQuit = null;

    private AnimationCurve randomHillCurve;
    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> previouslyHidden = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> previouslyHiddenMidFront = new HashSet<Vector3Int>();

    public static HashSet<Vector3Int> deletedTiles = new HashSet<Vector3Int>();
    private ChunkedWorldArchive worldArchive;
    private System.Random biomeRand;
    private Dictionary<Vector2Int, int> chunkBiomes = new();

    // --- Optimization: Cache player for Update checks
    private Vector3Int lastPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    void Awake()
    {
        string seedStr = customSeed;
        if (useCustomSeed && !string.IsNullOrEmpty(customSeed))
            generatedSeedHash = customSeed.GetHashCode();
        else if (!string.IsNullOrEmpty(hillRandomSeed) && hillRandomSeed.ToLower() != "random")
            generatedSeedHash = hillRandomSeed.GetHashCode();
        else
        {
            seedStr = DateTime.Now.Ticks.ToString();
            generatedSeedHash = seedStr.GetHashCode();
        }

        usedSeedString = seedStr;
        biomeRand = new System.Random(generatedSeedHash);

        ApplySeedRandomization();

        if (enableWorldArchive)
            worldArchive = new ChunkedWorldArchive(usedSeedString);
        else
            worldArchive = null;
    }

    void OnValidate()
    {
        usedSeedString = useCustomSeed && !string.IsNullOrEmpty(customSeed) ? customSeed : hillRandomSeed;
        ApplySeedRandomization();
    }

    [ContextMenu("Recalculate Seed Hash")]
    public void RecalculateSeedHash()
    {
        usedSeedString = useCustomSeed && !string.IsNullOrEmpty(customSeed) ? customSeed : hillRandomSeed;
        ApplySeedRandomization();
    }

    int HashSeed(string seed)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }

    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }
    int SeededInt(System.Random rand, int min, int max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return rand.Next(min, max);
    }

    public TileBase GetActualTileAssetAtCell(Vector3Int cell)
    {
        TileBase tile = null;
        if (frontTilemap != null)
            tile = frontTilemap.GetTile(cell);
        if (tile == null && middleFrontTilemap != null)
            tile = middleFrontTilemap.GetTile(cell);
        if (tile == null && middleBackTilemap != null)
            tile = middleBackTilemap.GetTile(cell);
        if (tile == null && backTilemap != null)
            tile = backTilemap.GetTile(cell);
        if (tile == null && groundTilemap != null)
            tile = groundTilemap.GetTile(cell);

        if (tile == null && enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(cell);
            if (tileData != null)
            {
                if (tileData.type == TileType.Grass)
                    tile = GetSurfaceTileAsset(cell, GetChunkBiome(cell.x / ChunkSize, cell.z));
                else if (tileData.type == TileType.Dirt)
                    tile = GetGroundTileAsset(GetChunkBiome(cell.x / ChunkSize, cell.z));
            }
        }
        return tile;
    }

    void ApplySeedRandomization()
    {
        int hash = HashSeed(usedSeedString);
        generatedSeedHash = hash;
        System.Random rand = new System.Random(hash);

        seedScale = SeededValue(rand, 0.05f, 0.2f, 1);
        seedAmplitude = SeededValue(rand, 0.6f, 3.0f, 2);
        hillHeight = SeededValue(rand, 3f, 15f, 4);
        hillCurveRandomJitter = SeededValue(rand, 0.08f, 0.7f, 5);
        hillRandomAmplitude = SeededValue(rand, 0.05f, 0.9f, 6);
        hillNoiseScale = SeededValue(rand, 0.25f, 1.3f, 7);
        curveShift = SeededValue(rand, -1.2f, 1.2f, 8);
        perlinOffsetX = SeededValue(rand, 0f, 100f, 9);
        perlinOffsetZ = SeededValue(rand, 0f, 100f, 10);
        perlinStrength = SeededValue(rand, 0.3f, 1.6f, 11);
        perlinBase = SeededValue(rand, 0f, 1.0f, 12);
        hillVerticalShift = SeededValue(rand, -2f, 2f, 13);
        cliffSharpness = SeededValue(rand, 1.5f, 3.0f, 14);
        buffer = SeededInt(rand, 2, 6, 15);
        worldBottomY = SeededInt(rand, -600, -200, 16);

        randomHillCurve = new AnimationCurve();
        int numKeys = SeededInt(rand, 8, 20, 17);

        for (int i = 0; i < numKeys; i++)
        {
            float t = Mathf.Lerp(0f, 1f, (float)i / (numKeys - 1));
            float canyonZ = Mathf.PerlinNoise(i * 0.23f + perlinOffsetZ, hash * 0.00001f) * 2f - 1f;
            float canyonWall = Mathf.Abs(canyonZ * hillRandomAmplitude * 2f);
            bool shouldBlock = canyonWall > 0.95f;

            float baseValue = Mathf.Sin(t * Mathf.PI * SeededValue(rand, 1f, 2f, 20 + i));
            float value = baseValue * cliffSharpness + SeededValue(rand, -hillCurveRandomJitter, hillCurveRandomJitter, 100 + i);

            value = Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), cliffSharpness);

            if (shouldBlock)
            {
                value = (rand.NextDouble() > 0.5) ? cliffSharpness * 1.5f : -cliffSharpness * 1.5f;
            }

            randomHillCurve.AddKey(new Keyframe(
                t,
                Mathf.Clamp(value, -cliffSharpness * 2f, cliffSharpness * 2f)
            ));
        }
    }

    private int GetChunkBiome(int chunkX, int z)
    {
        Vector2Int key = new Vector2Int(chunkX, z);
        if (!chunkBiomes.TryGetValue(key, out int biomeIndex))
        {
            if (biomes.Count == 0) return -1;
            biomeIndex = biomeRand.Next(0, biomes.Count);
            chunkBiomes[key] = biomeIndex;
        }
        return biomeIndex;
    }

    private TileBase GetSurfaceTileAsset(Vector3Int pos, int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return grassTileAsset;
        return biomes[biomeIndex].surfaceTile;
    }

    private TileBase GetGroundTileAsset(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return groundTileAsset;
        return biomes[biomeIndex].groundTile;
    }

    public float GetHillValue(int x, int z)
    {
        float layerOffset = Mathf.PerlinNoise(z * hillNoiseScale + perlinOffsetZ, curveShift * 0.29f) * 2f - 1f;
        float t = x * seedScale + curveShift + z * seedScale * 0.11f + layerOffset * hillRandomAmplitude * 2f;
        float tCurve = (t * 0.001f) + 0.5f;
        tCurve = Mathf.Clamp01(tCurve);
        float curveValue = randomHillCurve.Evaluate(tCurve) * seedAmplitude;

        float noiseValue = Mathf.PerlinNoise(x * hillNoiseScale + perlinOffsetX, z * hillNoiseScale + perlinOffsetZ);
        float extraNoise = Mathf.PerlinNoise(x * hillNoiseScale * 0.2f + perlinBase, z * hillNoiseScale * 0.2f + perlinBase) - 0.5f;
        noiseValue = (noiseValue - 0.5f) * perlinStrength + extraNoise * (hillRandomAmplitude * 0.7f);
        noiseValue = Mathf.Sign(noiseValue) * Mathf.Pow(Mathf.Abs(noiseValue), cliffSharpness);

        float canyonMask = Mathf.PerlinNoise(z * 0.12f + perlinOffsetZ * 0.5f, x * 0.015f + perlinOffsetX * 0.25f);
        if (canyonMask > 0.92f)
        {
            return hillHeight * 2f;
        }
        else if (canyonMask < 0.08f)
        {
            return -hillHeight * 2f;
        }

        float finalValue = curveValue + (noiseValue * hillRandomAmplitude) + hillVerticalShift;
        return finalValue;
    }

    public int GetSurfaceY(int x, int z) => Mathf.RoundToInt(GetHillValue(x, z) * hillHeight);

    private TileType GetTileType(Vector3Int pos, TileType fallback, bool isSurface)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            if (tileData != null)
                return tileData.type;
            var chosen = fallback;
            worldArchive.SetTile(pos, new TileData { type = chosen });
            return chosen;
        }
        return fallback;
    }

    private void SetTileForZ(Vector3Int pos, int z, int playerZ, TileBase tile)
    {
        foreach (var spacing in tilemapZSpacings)
            if (spacing.tilemap != null)
                spacing.tilemap.SetTile(pos, null);

        for (int i = 0; i < tilemapZSpacings.Count; i++)
        {
            if (tilemapZSpacings[i].tilemap != null && z == playerZ + (int)tilemapZSpacings[i].zSpacing)
            {
                tilemapZSpacings[i].tilemap.SetTile(pos, tile);
                break;
            }
        }
    }

    private void SpawnTileWithCaveCheck(int x, int y, int z, int playerZ, TileBase groundTile, TileCaveUtility caveUtil, int biomeIndex)
    {
        Vector3Int pos = new Vector3Int(x, y, z);
        int surfaceY = GetSurfaceY(x, z);
        bool isCave = caveUtil != null && caveUtil.IsCaveAt(x, y, z, surfaceY);

        if (isCave)
        {
            if (enableWorldArchive && worldArchive != null)
                worldArchive.SetTile(pos, new TileData { type = TileType.Air });

            SetTileForZ(pos, z, playerZ, null);
            return;
        }

        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            if (tileData != null)
            {
                if (tileData.type == TileType.Air)
                {
                    SetTileForZ(pos, z, playerZ, null);
                }
                else if (tileData.type == TileType.Grass)
                {
                    SetTileForZ(pos, z, playerZ, GetSurfaceTileAsset(pos, biomeIndex));
                }
                else
                {
                    SetTileForZ(pos, z, playerZ, groundTile);
                }
                return;
            }
        }

        if (enableWorldArchive && worldArchive != null)
            worldArchive.SetTile(pos, new TileData { type = TileType.Dirt });

        SetTileForZ(pos, z, playerZ, groundTile);
    }

    // These variables will help us track when to update the world.
    private Vector3Int lastTriggeredPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    private void Update()
    {
        if (playerTransform == null) return;
        if (groundTilemap == null || frontTilemap == null || middleFrontTilemap == null || middleBackTilemap == null || backTilemap == null) return;

        // Use integer cell positions for player
        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);

        // Only update if player has moved to a new cell since last update
        if (playerCell != lastTriggeredPlayerCell)
        {
            lastTriggeredPlayerCell = playerCell;
            UpdateWorldIfNeeded(playerCell);
        }
    }

    // This is your expensive logic, moved out of Update
    private void UpdateWorldIfNeeded(Vector3Int playerPos)
    {
        int playerZ = playerPos.z;
        int playerY = playerPos.y;

        lastPlayerPosForQuit = playerPos;

        float halfWidth = 10f; // Example render distance in X (modify as needed)
        int minX = playerPos.x - (int)halfWidth - buffer;
        int maxX = playerPos.x + (int)halfWidth + buffer;

        float halfCamHeight = 7f; // Example render distance in Y (modify as needed)
        int minY = playerY - (int)halfCamHeight - buffer;
        int maxY = playerY + (int)halfCamHeight + buffer;
        int buildBottom = Mathf.Min(minY, playerY);

        int playerChunk = Mathf.FloorToInt((float)playerPos.x / ChunkSize);
        int chunkGenLeft = playerChunk - ChunksGenerated;
        int chunkGenRight = playerChunk + ChunksGenerated;
        int chunkRenderLeft = playerChunk - (ChunksVisible / 2);
        int chunkRenderRight = playerChunk + (ChunksVisible / 2);

        int[] zs = new int[] { playerZ - 2, playerZ - 1, playerZ, playerZ + 1, playerZ + 2 };

        toRemove.Clear();
        foreach (var tile in activeTiles)
        {
            int chunkX = Mathf.FloorToInt((float)tile.x / ChunkSize);
            bool outOfChunk = chunkX < chunkRenderLeft || chunkX > chunkRenderRight;
            bool outOfZ = tile.z < playerZ - 2 || tile.z > playerZ + 2;
            bool belowWorldBottom = tile.y < worldBottomY;
            if (outOfChunk || outOfZ || belowWorldBottom)
            {
                groundTilemap.SetTile(tile, null);
                frontTilemap.SetTile(tile, null);
                middleFrontTilemap.SetTile(tile, null);
                middleBackTilemap.SetTile(tile, null);
                backTilemap.SetTile(tile, null);
                toRemove.Add(tile);
            }
        }
        foreach (var tile in toRemove)
            activeTiles.Remove(tile);

        for (int z_i = 0; z_i < zs.Length; z_i++)
        {
            int z = zs[z_i];
            for (int chunkX = chunkGenLeft; chunkX <= chunkGenRight; chunkX++)
            {
                bool render = chunkX >= chunkRenderLeft && chunkX <= chunkRenderRight;
                SpawnOrLoadChunk(chunkX, z, buildBottom, maxY, playerZ, render);
            }
        }

        if (tileHiddenSet != null)
        {
            var currentlyHidden = tileHiddenSet.GetTilesToHide(playerTransform.position);

            foreach (var pos in currentlyHidden)
                frontTilemap.SetTile(pos, hideTileAsset);
            foreach (var pos in previouslyHidden)
                if (!currentlyHidden.Contains(pos))
                    frontTilemap.SetTile(pos, null);

            foreach (var pos in currentlyHidden)
                middleBackTilemap.SetTile(pos, hideTileAsset);
            foreach (var pos in previouslyHidden)
                if (!currentlyHidden.Contains(pos))
                    middleBackTilemap.SetTile(pos, null);

            previouslyHidden = currentlyHidden;
        }

        if (midFrontTileHiddenSet != null)
        {
            var currentlyHiddenMidFront = midFrontTileHiddenSet.GetTilesToHide(playerTransform.position);

            foreach (var pos in currentlyHiddenMidFront)
                middleFrontTilemap.SetTile(pos, hideTileAsset);
            foreach (var pos in previouslyHiddenMidFront)
                if (!currentlyHiddenMidFront.Contains(pos))
                    middleFrontTilemap.SetTile(pos, null);

            previouslyHiddenMidFront = currentlyHiddenMidFront;
        }

        UpdateHillCurvePreview(playerZ);

        // DO NOT refresh or manage any active tiles for overlays here!
        // That logic is now handled by the overlay service scripts.
    }

    public void SpawnOrLoadChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render)
    {
        int biomeIndex = GetChunkBiome(chunkX, z);

        int startX = chunkX * ChunkSize;
        int endX = startX + ChunkSize - 1;

        // Prepare batch lists for each tilemap in tilemapZSpacings
        var positions = new List<Vector3Int>[tilemapZSpacings.Count];
        var tiles = new List<TileBase>[tilemapZSpacings.Count];
        for (int i = 0; i < tilemapZSpacings.Count; i++)
        {
            positions[i] = new List<Vector3Int>();
            tiles[i] = new List<TileBase>();
        }

        for (int x = startX; x <= endX; x++)
        {
            float hillValue = GetHillValue(x, z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            // Surface tile
            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            if (render && !deletedTiles.Contains(surfacePos))
            {
                BatchSpawnFromArchiveOrGenerate(
                    surfacePos, x, surfaceY, z, playerZ,
                    GetSurfaceTileAsset(surfacePos, biomeIndex),
                    caveUtility, biomeIndex,
                    positions, tiles, isSurface: true
                );
                activeTiles.Add(surfacePos);
            }
            // Dirt below
            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                if (render && !deletedTiles.Contains(dirtPos))
                {
                    BatchSpawnFromArchiveOrGenerate(
                        dirtPos, x, y, z, playerZ,
                        GetGroundTileAsset(biomeIndex),
                        caveUtility, biomeIndex,
                        positions, tiles, isSurface: false
                    );
                    activeTiles.Add(dirtPos);
                }
            }
        }

        // Draw all tiles for each tilemap in batch
        for (int i = 0; i < tilemapZSpacings.Count; i++)
        {
            if (tilemapZSpacings[i].tilemap != null && positions[i].Count > 0)
                tilemapZSpacings[i].tilemap.SetTiles(positions[i].ToArray(), tiles[i].ToArray());
        }
    }

    // Helper: Spawn tile using archive if exists, otherwise generate and store to archive, then batch-draw
    private void BatchSpawnFromArchiveOrGenerate(
        Vector3Int pos, int x, int y, int z, int playerZ,
        TileBase defaultTile, TileCaveUtility caveUtil, int biomeIndex,
        List<Vector3Int>[] positions, List<TileBase>[] tiles, bool isSurface)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            TileType resolvedType;
            TileBase resolvedTile = null;

            // Use archive if exists
            if (tileData != null)
            {
                resolvedType = tileData.type;
                if (tileData.type == TileType.Air)
                    resolvedTile = null;
                else if (tileData.type == TileType.Grass)
                    resolvedTile = GetSurfaceTileAsset(pos, biomeIndex);
                else
                    resolvedTile = GetGroundTileAsset(biomeIndex);
            }
            else
            {
                // Not in archive, must generate
                int surfaceY = GetSurfaceY(x, z);
                bool isCave = caveUtil != null && caveUtil.IsCaveAt(x, y, z, surfaceY);

                if (isCave)
                {
                    resolvedType = TileType.Air;
                    resolvedTile = null;
                }
                else if (isSurface)
                {
                    resolvedType = TileType.Grass;
                    resolvedTile = GetSurfaceTileAsset(pos, biomeIndex);
                }
                else
                {
                    resolvedType = TileType.Dirt;
                    resolvedTile = GetGroundTileAsset(biomeIndex);
                }
                // Store in archive for future
                worldArchive.SetTile(pos, new TileData { type = resolvedType });
            }

            // Find target tilemap index for this Z
            for (int i = 0; i < tilemapZSpacings.Count; i++)
            {
                var spacing = tilemapZSpacings[i];
                if (spacing.tilemap != null && z == playerZ + (int)spacing.zSpacing)
                {
                    positions[i].Add(pos);
                    tiles[i].Add(resolvedTile);
                    break;
                }
            }
        }
        else
        {
            // Archive not enabled: use old logic
            int surfaceY = GetSurfaceY(x, z);
            bool isCave = caveUtil != null && caveUtil.IsCaveAt(x, y, z, surfaceY);
            TileBase tileToDraw = isCave ? null : defaultTile;

            for (int i = 0; i < tilemapZSpacings.Count; i++)
            {
                var spacing = tilemapZSpacings[i];
                if (spacing.tilemap != null && z == playerZ + (int)spacing.zSpacing)
                {
                    positions[i].Add(pos);
                    tiles[i].Add(tileToDraw);
                    break;
                }
            }
        }
    }

    private void UpdateHillCurvePreview(int zLayer)
    {
        if (playerTransform == null) return;
        if (hillCurve == null) hillCurve = new AnimationCurve();

        const int sampleCount = 100;
        const float viewWidth = 50f;
        float playerX = playerTransform.position.x;
        float startX = playerX - viewWidth * 0.5f;
        float endX = playerX + viewWidth * 0.5f;

        var keys = new Keyframe[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            float x = Mathf.Lerp(startX, endX, t);
            float y = GetHillValue(Mathf.RoundToInt(x), zLayer);
            keys[i] = new Keyframe(t, y);
        }
        hillCurve.keys = keys;
    }

    // Remove all overlay/active tile management for overlays!
    // overlays should use their own services for visible/active cell management.

    public bool IsTileHidden(Vector3Int pos)
    {
        if (tileHiddenSet == null || playerTransform == null) return false;
        var currentlyHidden = tileHiddenSet.GetTilesToHide(playerTransform.position);
        return currentlyHidden != null && currentlyHidden.Contains(pos);
    }
    public void ModifyTile(Vector3Int pos, TileType type)
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SetTile(pos, new TileData { type = type });
    }
    public void DeleteTile(Vector3Int pos)
    {
        deletedTiles.Add(pos);
        if (groundTilemap != null) groundTilemap.SetTile(pos, null);
        if (frontTilemap != null) frontTilemap.SetTile(pos, null);
        if (middleFrontTilemap != null) middleFrontTilemap.SetTile(pos, null);
        if (middleBackTilemap != null) middleBackTilemap.SetTile(pos, null);
        if (backTilemap != null) backTilemap.SetTile(pos, null);

        if (enableWorldArchive && worldArchive != null)
            worldArchive.RemoveTile(pos);
    }
    public void SaveGame()
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SaveAll();
    }
    public TileType GetTileTypeForFog(Vector3Int pos, TileType fallback)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            if (tileData != null)
                return tileData.type;
        }
        float hillValue = GetHillValue(pos.x, pos.z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
        if (pos.y > surfaceY)
            return TileType.Air;
        if (pos.y == surfaceY)
            return TileType.Grass;
        return TileType.Dirt;
    }
}