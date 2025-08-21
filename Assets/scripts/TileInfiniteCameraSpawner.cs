
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [Header("Hill Randomization")]
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
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap frontTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap backTilemap;
    [SerializeField] private TileBase groundTileAsset;
    [SerializeField] private TileBase grassTileAsset;
    [SerializeField] private TileBase hideTileAsset;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int buffer;

    [Header("Advanced Seed Controls")]
    [SerializeField] private float repeatRange;
    [SerializeField] private float curveShift;
    [SerializeField] private float perlinOffsetX;
    [SerializeField] private float perlinOffsetZ;
    [SerializeField] private float perlinStrength;
    [SerializeField] private float perlinBase;

    [Header("World Controls")]
    [SerializeField] private int worldBottomY;

    [Header("Hidden Tiles")]
    [SerializeField] private TileHiddenSet tileHiddenSet; // assign in Inspector

    private AnimationCurve randomHillCurve;
    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> previouslyHidden = new HashSet<Vector3Int>();

    // --- Deleted tiles tracker ---
    public static HashSet<Vector3Int> deletedTiles = new HashSet<Vector3Int>();

    // --- Chunked archive ---
    private ChunkedWorldArchive worldArchive;
    private const int ChunkSize = 16;
    private const int ChunksVisible = 3; // one left, center, one right
    private const int ChunksGenerated = 6; // chunks kept loaded left/right of player

    void Awake()
    {
        if (string.IsNullOrEmpty(hillRandomSeed) || hillRandomSeed.ToLower() == "random")
        {
            hillRandomSeed = DateTime.Now.Ticks.ToString();
        }
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
        worldArchive = new ChunkedWorldArchive(usedSeedString);
    }

    void OnValidate()
    {
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
    }

    [ContextMenu("Recalculate Seed Hash")]
    public void RecalculateSeedHash()
    {
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
    }

    public int GetSurfaceY(int x, int z)
    {
        float hillValue = GetHillValue(x, z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
        return surfaceY;
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

    private void Start()
    {
        Debug.Log($"Hill Value at 0, 0: {GetHillValue(0, 0)}");
        Debug.Log($"Hill Value at 10, 10: {GetHillValue(10, 10)}");
        Debug.Log($"Hill Value at 20, 20: {GetHillValue(20, 20)}");
    }

    void ApplySeedRandomization()
    {
        int hash = HashSeed(usedSeedString);
        generatedSeedHash = hash;

        System.Random rand = new System.Random(hash);

        // All params affect curve
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

        // Generate curve with sharp cliffs and possible blockages
        for (int i = 0; i < numKeys; i++)
        {
            float t = Mathf.Lerp(0f, 1f, (float)i / (numKeys - 1));
            // Add Z influence for canyon/closure
            float canyonZ = Mathf.PerlinNoise(i * 0.23f + perlinOffsetZ, hash * 0.00001f) * 2f - 1f;
            float canyonWall = Mathf.Abs(canyonZ * hillRandomAmplitude * 2f);
            bool shouldBlock = canyonWall > 0.95f; // very steep wall if Perlin is high

            float baseValue = Mathf.Sin(t * Mathf.PI * SeededValue(rand, 1f, 2f, 20 + i));
            // Apply cliff sharpness and random jitter, and canyon wall effect
            float value = baseValue * cliffSharpness + SeededValue(rand, -hillCurveRandomJitter, hillCurveRandomJitter, 100 + i);

            // Sharper cliffs
            value = Mathf.Sign(value) * Mathf.Pow(Mathf.Abs(value), cliffSharpness);

            if (shouldBlock)
            {
                // Force a wall: high or low depending on random
                value = (rand.NextDouble() > 0.5) ? cliffSharpness * 1.5f : -cliffSharpness * 1.5f;
            }

            randomHillCurve.AddKey(new Keyframe(
                t,
                Mathf.Clamp(value, -cliffSharpness * 2f, cliffSharpness * 2f)
            ));
        }
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

        // --- New: Z-canyon closing logic ---
        float canyonMask = Mathf.PerlinNoise(z * 0.12f + perlinOffsetZ * 0.5f, x * 0.015f + perlinOffsetX * 0.25f);
        if (canyonMask > 0.92f)
        {
            // "Close off" canyon with a tall wall
            return hillHeight * 2f;
        }
        else if (canyonMask < 0.08f)
        {
            // Deep drop/crater
            return -hillHeight * 2f;
        }

        float finalValue = curveValue + (noiseValue * hillRandomAmplitude) + hillVerticalShift;
        return finalValue;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (playerTransform == null) return;
        if (groundTilemap == null || frontTilemap == null || middleFrontTilemap == null || middleBackTilemap == null || backTilemap == null || groundTileAsset == null || grassTileAsset == null) return;

        Vector3Int playerPos = Vector3Int.FloorToInt(playerTransform.position);
        int playerZ = playerPos.z;
        int playerY = Mathf.FloorToInt(playerTransform.position.y);

        Vector3 camPos = cam.transform.position;
        float halfWidth = cam.orthographicSize * cam.aspect;
        int minX = Mathf.FloorToInt(camPos.x - halfWidth) - buffer;
        int maxX = Mathf.CeilToInt(camPos.x + halfWidth) + buffer;

        float camY = cam.transform.position.y;
        float halfCamHeight = cam.orthographicSize;
        int minY = Mathf.FloorToInt(camY - halfCamHeight) - buffer;
        int maxY = Mathf.CeilToInt(camY + halfCamHeight) + buffer;
        int buildBottom = Mathf.Min(minY, playerY);

        // --- Chunked world logic ---
        int playerChunk = Mathf.FloorToInt((float)playerPos.x / ChunkSize);
        int chunkGenLeft = playerChunk - ChunksGenerated;
        int chunkGenRight = playerChunk + ChunksGenerated;
        int chunkRenderLeft = playerChunk - (ChunksVisible / 2);
        int chunkRenderRight = playerChunk + (ChunksVisible / 2);

        int[] zs = new int[] { playerZ - 2, playerZ - 1, playerZ, playerZ + 1, playerZ + 2 };

        // Remove out-of-range tiles (by chunk)
        List<Vector3Int> toRemove = new List<Vector3Int>();
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

        // Generate & render chunks
        for (int z_i = 0; z_i < zs.Length; z_i++)
        {
            int z = zs[z_i];
            for (int chunkX = chunkGenLeft; chunkX <= chunkGenRight; chunkX++)
            {
                bool render = chunkX >= chunkRenderLeft && chunkX <= chunkRenderRight;
                SpawnOrLoadChunk(chunkX, z, buildBottom, maxY, playerZ, render);
            }
        }

        // --- Unload distant chunks to keep memory usage low ---
        worldArchive.UnloadDistantChunks(playerPos);

        // --- Collider setup: only groundTilemap gets colliders ---
        foreach (var tile in activeTiles)
        {
            // Set colliders for each tilemap according to tile.z
            if (groundTilemap != null && tile.z == playerZ)
                groundTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            else if (middleFrontTilemap != null && tile.z == playerZ - 1)
                middleFrontTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            else if (middleBackTilemap != null && tile.z == playerZ + 1)
                middleBackTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            else if (frontTilemap != null && tile.z == playerZ - 2)
                frontTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            else if (backTilemap != null && tile.z == playerZ + 2)
                backTilemap.SetColliderType(tile, Tile.ColliderType.Grid);

            // Optionally, clear colliders on other tilemaps for this tile if you want to ensure no stray colliders:
            if (groundTilemap != null && tile.z != playerZ)
                groundTilemap.SetColliderType(tile, Tile.ColliderType.None);
            if (middleFrontTilemap != null && tile.z != playerZ - 1)
                middleFrontTilemap.SetColliderType(tile, Tile.ColliderType.None);
            if (middleBackTilemap != null && tile.z != playerZ + 1)
                middleBackTilemap.SetColliderType(tile, Tile.ColliderType.None);
            if (frontTilemap != null && tile.z != playerZ - 2)
                frontTilemap.SetColliderType(tile, Tile.ColliderType.None);
            if (backTilemap != null && tile.z != playerZ + 2)
                backTilemap.SetColliderType(tile, Tile.ColliderType.None);
        }

        // ==== HIDDEN TILE LOGIC ====
        if (tileHiddenSet != null)
        {
            var currentlyHidden = tileHiddenSet.GetTilesToHide(playerTransform.position);

            // Hide new tiles
            foreach (var pos in currentlyHidden)
            {
                frontTilemap.SetTile(pos, hideTileAsset);
                middleFrontTilemap.SetTile(pos, hideTileAsset);
                frontTilemap.SetColliderType(pos, Tile.ColliderType.None);
                middleFrontTilemap.SetColliderType(pos, Tile.ColliderType.None);
            }
            // Unhide tiles no longer hidden
            foreach (var pos in previouslyHidden)
            {
                if (!currentlyHidden.Contains(pos))
                {
                    frontTilemap.SetTile(pos, null);
                    middleFrontTilemap.SetTile(pos, null);
                    frontTilemap.SetColliderType(pos, Tile.ColliderType.None);
                    middleFrontTilemap.SetColliderType(pos, Tile.ColliderType.None);
                }
            }
            previouslyHidden = currentlyHidden;
        }

        // ==== HILL CURVE LIVE PREVIEW ====
        UpdateHillCurvePreview(playerZ);
    }

    // Generate or load a chunk (archive-aware)
    private void SpawnOrLoadChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render)
    {
        int startX = chunkX * ChunkSize;
        int endX = startX + ChunkSize - 1;

        for (int x = startX; x <= endX; x++)
        {
            float hillValue = GetHillValue(x, z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            TileType tileType = GetTileType(surfacePos, TileType.Grass, true);

            if (render && tileType != TileType.Air && !deletedTiles.Contains(surfacePos))
            {
                SetTileForZ(surfacePos, z, playerZ, tileType == TileType.Grass ? grassTileAsset : groundTileAsset);
                activeTiles.Add(surfacePos);
            }

            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                TileType dirtType = GetTileType(dirtPos, TileType.Dirt, false);

                if (render && dirtType != TileType.Air && !deletedTiles.Contains(dirtPos))
                {
                    SetTileForZ(dirtPos, z, playerZ, groundTileAsset);
                    activeTiles.Add(dirtPos);
                }
            }
        }
    }
    public TileType GetTileTypeForFog(Vector3Int pos, TileType fallback)
    {
        TileData tileData = worldArchive.TryGetTile(pos);
        if (tileData != null)
            return tileData.type;
        // Procedural fallback (optional, can be improved for caves etc.)
        float hillValue = GetHillValue(pos.x, pos.z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
        if (pos.y > surfaceY)
            return TileType.Air;
        if (pos.y == surfaceY)
            return TileType.Grass;
        return TileType.Dirt;
    }

    // Check archive for tile type, else use fallback (procedural)
    private TileType GetTileType(Vector3Int pos, TileType fallback, bool isSurface)
    {
        TileData tileData = worldArchive.TryGetTile(pos);
        if (tileData != null)
            return tileData.type;
        // procedural fallback
        return fallback;
    }

    // Helper for setting tile in the correct tilemap based on z
    private void SetTileForZ(Vector3Int pos, int z, int playerZ, TileBase tile)
    {
        groundTilemap.SetTile(pos, null);
        frontTilemap.SetTile(pos, null);
        middleFrontTilemap.SetTile(pos, null);
        middleBackTilemap.SetTile(pos, null);
        backTilemap.SetTile(pos, null);

        if (z == playerZ - 2)
            frontTilemap.SetTile(pos, tile);
        else if (z == playerZ - 1)
            middleFrontTilemap.SetTile(pos, tile);
        else if (z == playerZ)
            groundTilemap.SetTile(pos, tile);
        else if (z == playerZ + 1)
            middleBackTilemap.SetTile(pos, tile);
        else if (z == playerZ + 2)
            backTilemap.SetTile(pos, tile);
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

    public HashSet<Vector3Int> GetActiveTiles()
    {
        return activeTiles;
    }

    public bool IsTileHidden(Vector3Int pos)
    {
        if (tileHiddenSet == null) return false;
        if (playerTransform == null) return false;

        var currentlyHidden = tileHiddenSet.GetTilesToHide(playerTransform.position);
        return currentlyHidden != null && currentlyHidden.Contains(pos);
    }

    // Player modifies a tile (e.g. digging/building)
    public void ModifyTile(Vector3Int pos, TileType type)
    {
        worldArchive.SetTile(pos, new TileData { type = type });
    }

    // Player deletes a tile (removes from both world and archive)
    public void DeleteTile(Vector3Int pos)
    {
        deletedTiles.Add(pos);
        worldArchive.RemoveTile(pos);
    }

    // Manual save, e.g. call from UI or gameplay
    public void SaveGame()
    {
        if (worldArchive != null)
            worldArchive.SaveAll();
    }
}