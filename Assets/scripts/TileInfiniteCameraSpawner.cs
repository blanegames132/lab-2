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

    public TileBase surfaceTile;     // e.g. grass, sand, snow
    public TileBase subsurfaceTile;  // e.g. soil, sand, ice (just under surface)
    public TileBase groundTile;      // e.g. stone, sandstone, frozen dirt
    public TileBase bedrockTile;     // unbreakable bottom
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
    public List<Tilemap> biomeTilemaps; // Assign in inspector

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
    [SerializeField] public int ChunkSize = 16;
    [SerializeField] private int ChunksVisible = 3;
    [SerializeField] private int ChunksGenerated = 6;

    [Header("Hidden Tiles")]
    [SerializeField] private TileHiddenSet tileHiddenSet;
    [SerializeField] private MidFrontTileHiddenSet midFrontTileHiddenSet;

    private static readonly List<Vector3Int> toRemove = new List<Vector3Int>();
    private Vector3Int? lastPlayerPosForQuit = null;

    private AnimationCurve randomHillCurve;
    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> previouslyHidden = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> previouslyHiddenMidFront = new HashSet<Vector3Int>();

    public static HashSet<Vector3Int> deletedTiles = new HashSet<Vector3Int>();
    private ChunkedWorldArchive worldArchive;
    private System.Random biomeRand;
    private Dictionary<Vector2Int, int> chunkBiomes = new();

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
    private TileBase ResolveTileFromTag(Vector3Int pos, int biomeIndex, string tag)
    {
        if (tag.StartsWith("surface:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].surfaceTile;

        if (tag.StartsWith("subsurface:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].subsurfaceTile;

        if (tag.StartsWith("ground:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].groundTile;

        if (tag.StartsWith("bedrock:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].bedrockTile;

        if (tag == "unt1")
            return biomes[biomeIndex].surfaceTile;

        if (tag == "unt2")
            return biomes[biomeIndex].groundTile;

        return null;
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
                int biomeIndex = GetChunkBiome(cell.x / ChunkSize, cell.z);
                tile = ResolveTileFromTag(cell, biomeIndex, tileData.blockTagOrName);
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
    private int GetChunkBuffer(int chunkX, int z)
    {
        // Use world seed and chunk coordinates to get a deterministic buffer value per chunk
        int hash = generatedSeedHash ^ (chunkX * 73856093) ^ (z * 19349663);
        System.Random rand = new System.Random(hash);
        return rand.Next(3, 11); // 3 to 10 inclusive
    }

    public int GetChunkBiome(int chunkX, int z)
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

    private string GetBiomeTag(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return "untagged";
        return biomes[biomeIndex].name.Trim().ToLower();
    }

    private TileBase GetSurfaceTileAsset(Vector3Int pos, int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return null; // No biome, no tile
        return biomes[biomeIndex].surfaceTile;
    }

    private TileBase GetGroundTileAsset(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return null; // No biome, no tile
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

    private void ArchiveTileIfNeeded(
        Vector3Int pos,
        int x,
        int y,
        int z,
        int biomeIndex,
        bool isSurface,
        int chunkX,
        Vector3Int playerPos,
        float halfWidth
    )
    {
        if (!(enableWorldArchive && worldArchive != null)) return;
        if (worldArchive.TryGetTile(pos) != null) return; // already archived

        int surfaceY = GetSurfaceY(x, z);
        bool isCave = caveUtility != null && caveUtility.IsCaveAt(x, y, z, surfaceY);
        int buffer = GetChunkBuffer(chunkX, z);
        int minX = playerPos.x - (int)halfWidth - buffer;
        int maxX = playerPos.x + (int)halfWidth + buffer;
        string tag = null;

        if (isCave)
        {
            tag = "cave";
        }
        else if (y == surfaceY)
        {
            tag = "surface:" + GetBiomeTag(biomeIndex);
        }
        else if (y < surfaceY && y >= surfaceY - 3)
        {
            tag = "subsurface:" + GetBiomeTag(biomeIndex);
        }
        else if (y < surfaceY - 3 && y > worldBottomY)
        {
            tag = "ground:" + GetBiomeTag(biomeIndex);
        }
        else if (y == worldBottomY)
        {
            tag = "bedrock:" + GetBiomeTag(biomeIndex);
        }
        else
        {
            tag = "air";
        }

        if (string.IsNullOrEmpty(tag))
            tag = (UnityEngine.Random.value > 0.5f) ? "unt1" : "unt2";

        worldArchive.SetTile(pos, new TileData { blockTagOrName = tag });
    }

    private void BatchDrawFromArchive(
        Vector3Int pos, int z, int playerZ, int biomeIndex,
        List<Vector3Int>[] positions, List<TileBase>[] tiles)
    {
        if (!(enableWorldArchive && worldArchive != null)) return;

        TileData tileData = worldArchive.TryGetTile(pos);
        if (tileData == null) return; // Only draw if archived

        TileBase resolvedTile = ResolveTileFromTag(pos, biomeIndex, tileData.blockTagOrName);

        // Place into the correct z-spaced tilemap
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

    private Vector3Int lastTriggeredPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    private void Update()
    {
        if (playerTransform == null) return;
        if (groundTilemap == null || frontTilemap == null || middleFrontTilemap == null || middleBackTilemap == null || backTilemap == null) return;

        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);

        if (playerCell != lastTriggeredPlayerCell)
        {
            lastTriggeredPlayerCell = playerCell;
            UpdateWorldIfNeeded(playerCell);
        }
    }

    private void UpdateWorldIfNeeded(Vector3Int playerPos)
    {
        int playerZ = playerPos.z;
        int playerY = playerPos.y;

        lastPlayerPosForQuit = playerPos;

        float halfWidth = 10f;
        int minX = playerPos.x - (int)halfWidth - buffer;
        int maxX = playerPos.x + (int)halfWidth + buffer;

        float halfCamHeight = 7f;
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
        if (enableWorldArchive && worldArchive != null && caveUtility != null)
        {
            caveUtility.DrawCavesFromArchive(worldArchive);
        }

        UpdateHillCurvePreview(playerZ);
    }

    public void SpawnOrLoadChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render)
    {
        int biomeIndex = GetChunkBiome(chunkX, z);

        int startX = chunkX * ChunkSize;
        int endX = startX + ChunkSize - 1;

        // Get player position and halfWidth for buffer calculation
        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);
        float halfWidth = 10f; // Use your preferred width value here

        for (int x = startX; x <= endX; x++)
        {
            float hillValue = GetHillValue(x, z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            if (render && !deletedTiles.Contains(surfacePos))
            {
                ArchiveTileIfNeeded(surfacePos, x, surfaceY, z, biomeIndex, isSurface: true, chunkX, playerCell, halfWidth);
                activeTiles.Add(surfacePos);
            }
            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                if (render && !deletedTiles.Contains(dirtPos))
                {
                    ArchiveTileIfNeeded(dirtPos, x, y, z, biomeIndex, isSurface: false, chunkX, playerCell, halfWidth);
                    activeTiles.Add(dirtPos);
                }
            }
        }
    

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

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            if (render && !deletedTiles.Contains(surfacePos))
                BatchDrawFromArchive(surfacePos, z, playerZ, biomeIndex, positions, tiles);

            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                if (render && !deletedTiles.Contains(dirtPos))
                    BatchDrawFromArchive(dirtPos, z, playerZ, biomeIndex, positions, tiles);
            }
        }

        for (int i = 0; i < tilemapZSpacings.Count; i++)
        {
            if (tilemapZSpacings[i].tilemap != null && positions[i].Count > 0)
                tilemapZSpacings[i].tilemap.SetTiles(positions[i].ToArray(), tiles[i].ToArray());
        }
    }
    public class ChunkUpdateManager : MonoBehaviour
    {
        public TileInfiniteCameraSpawner spawner;
        public int maxChunksPerFrame = 2; // Limit for performance
        public float updateDistance = 64f; // Only update chunks within this distance

        private Queue<ChunkRequest> chunkQueue = new Queue<ChunkRequest>();

        void Update()
        {
            Vector3 playerPos = spawner.playerTransform.position;

            // Remove queued chunks if too far from player
            int count = chunkQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var req = chunkQueue.Dequeue();
                float dist = Vector3.Distance(playerPos, req.centerWorldPos);
                if (dist < updateDistance)
                    chunkQueue.Enqueue(req);
            }

            // Process a few chunks per frame
            int processed = 0;
            while (chunkQueue.Count > 0 && processed < maxChunksPerFrame)
            {
                var req = chunkQueue.Dequeue();
                float dist = Vector3.Distance(playerPos, req.centerWorldPos);
                if (dist < updateDistance)
                {
                    spawner.SpawnOrLoadChunk(req.chunkX, req.z, req.buildBottom, req.maxY, req.playerZ, req.render);
                    processed++;
                }
            }
        }

        public void RequestChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render, Vector3 centerWorldPos)
        {
            chunkQueue.Enqueue(new ChunkRequest
            {
                chunkX = chunkX,
                z = z,
                buildBottom = buildBottom,
                maxY = maxY,
                playerZ = playerZ,
                render = render,
                centerWorldPos = centerWorldPos
            });
        }

        private struct ChunkRequest
        {
            public int chunkX, z, buildBottom, maxY, playerZ;
            public bool render;
            public Vector3 centerWorldPos;
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

    public bool IsTileHidden(Vector3Int pos)
    {
        if (tileHiddenSet == null || playerTransform == null) return false;
        var currentlyHidden = tileHiddenSet.GetTilesToHide(playerTransform.position);
        return currentlyHidden != null && currentlyHidden.Contains(pos);
    }

    public void ModifyTile(Vector3Int pos, string tag)
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SetTile(pos, new TileData { blockTagOrName = tag });
    }

    public void DeleteTile(Vector3Int pos)
    {
        // Remove from groundTilemap
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);

        // Remove from all biome tilemaps
        if (biomeTilemaps != null)
        {
            foreach (var tilemap in biomeTilemaps)
            {
                if (tilemap != null)
                    tilemap.SetTile(pos, null);
            }
        }

        // Remove from world archive if enabled
        if (enableWorldArchive && worldArchive != null)
            worldArchive.RemoveTile(pos);

        deletedTiles.Add(pos);
    }


public void SaveGame()
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SaveAll();
    }

    public void RefreshGroundTile(Vector3Int pos)
    {
        int biomeIndex = GetChunkBiome(pos.x / ChunkSize, pos.z);

        // Try archive first
        TileData tileData = (enableWorldArchive && worldArchive != null) ? worldArchive.TryGetTile(pos) : null;
        TileBase tile = null;

        if (tileData != null)
        {
            tile = ResolveTileFromTag(pos, biomeIndex, tileData.blockTagOrName);
        }
        else
        {
            // Procedural fallback if not archived
            float hillValue = GetHillValue(pos.x, pos.z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            if (pos.y == surfaceY)
                tile = GetSurfaceTileAsset(pos, biomeIndex);  // surface
            else if (pos.y < surfaceY)
                tile = GetGroundTileAsset(biomeIndex);       // ground
            else
                tile = null;                                 // air
        }

        if (groundTilemap != null)
            groundTilemap.SetTile(pos, tile);
    }

    public string GetTileTypeForFog(Vector3Int pos, string fallback, int biomeIndex)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            if (tileData != null) return tileData.blockTagOrName;
        }

        float hillValue = GetHillValue(pos.x, pos.z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

        if (pos.y > surfaceY) return "air";
        if (caveUtility != null && caveUtility.IsCaveAt(pos.x, pos.y, pos.z, surfaceY)) return "cave";
        return (pos.y == surfaceY) ? GetBiomeTag(biomeIndex) : "ground:" + GetBiomeTag(biomeIndex);
    }


    public string GetTileTagForFog(Vector3Int pos, string fallback, int biomeIndex)
    {
        GetTileTypeForFog(pos, fallback, biomeIndex);
        if (enableWorldArchive && worldArchive != null)
        {
            TileData tileData = worldArchive.TryGetTile(pos);
            if (tileData != null)
                return tileData.blockTagOrName;
        }
        float hillValue = GetHillValue(pos.x, pos.z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
        if (pos.y > surfaceY)
            return "air";
        if (caveUtility != null && caveUtility.IsCaveAt(pos.x, pos.y, pos.z, surfaceY))
            return "cave";
        return (pos.y == surfaceY) ? GetBiomeTag(biomeIndex) : "ground:" + GetBiomeTag(biomeIndex);
    }
}
