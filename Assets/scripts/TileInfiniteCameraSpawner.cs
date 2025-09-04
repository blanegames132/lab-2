using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TilemapZSpacing
{
    public Tilemap tilemap;
    public float zSpacing = 1f;
}

public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [Header("Seed")]
    [SerializeField] public SeedSelector seedSelector;
    [SerializeField] public WorldSeedApplier worldSeedApplier;

    [Header("Archive")]
    [SerializeField] public WorldArchiveManager worldArchiveManager;

    [Header("Biomes")]
    [SerializeField] public BiomeManager biomeManager;

    [Header("Seed Settings")]
    [SerializeField] public bool enableWorldArchive = true;

    [Header("Hill Shape Controls")]
    [SerializeField] private AnimationCurve hillCurve;
    [SerializeField] public float hillHeight;
    [SerializeField] public float cliffSharpness;
    [SerializeField] public float seedAmplitude;
    [SerializeField] public float seedScale;
    [SerializeField] public float hillNoiseScale;
    [SerializeField] public float hillCurveRandomJitter;
    [SerializeField] public float hillRandomAmplitude;
    [SerializeField] public float hillVerticalShift;

    public HashSet<Vector3Int> pendingDeletion = new HashSet<Vector3Int>();

    [Header("World Build Controls")]
    [SerializeField, Min(1)] public int buildDepthBelowPlayer = 50;

    [Header("Tilemap Setup")]
    [SerializeField] public Tilemap groundTilemap;
    [SerializeField] public Tilemap frontTilemap;
    [SerializeField] public Tilemap middleFrontTilemap;
    [SerializeField] public Tilemap middleBackTilemap;
    [SerializeField] public Tilemap backTilemap;

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

    [Header("Cave Settings")]
    [SerializeField] public TileCaveUtility caveUtility;

    [Header("Advanced Seed Controls")]
    [SerializeField] public float repeatRange;
    [SerializeField] public float curveShift;
    [SerializeField] public float perlinOffsetX;
    [SerializeField] public float perlinOffsetZ;
    [SerializeField] public float perlinStrength;
    [SerializeField] public float perlinBase;

    [Header("World Controls")]
    [SerializeField] public int worldBottomY = -100;

    [Header("Chunk Settings")]
    [SerializeField] public int ChunkSize = 16;
    [SerializeField] public int ChunksVisible = 3;
    [SerializeField] public int ChunksGenerated = 6;

    [Header("Hidden Tiles")]
    public static readonly List<Vector3Int> toRemove = new List<Vector3Int>();
    public Vector3Int? lastPlayerPosForQuit = null;
    public AnimationCurve randomHillCurve;
    public HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> previouslyHidden = new HashSet<Vector3Int>();
    public HashSet<Vector3Int> previouslyHiddenMidFront = new HashSet<Vector3Int>();

    [Header("Chunk Update Manager")]
    [SerializeField] public ChunkUpdateManager chunkUpdateManager;
    public static HashSet<Vector3Int> deletedTiles = new HashSet<Vector3Int>();

    public System.Random biomeRand;
    public Dictionary<Vector2Int, int> chunkBiomes = new();

    [Header("Delete Queue Tilemap")]
    [SerializeField] public Tilemap deleteQueueTilemap;

    public Queue<Vector3Int> deleteQueuePositions = new Queue<Vector3Int>();

    public void QueueTileForDeleteQueueTilemap(Vector3Int pos)
    {
        if (deleteQueueTilemap != null && !pendingDeletion.Contains(pos))
        {
            deleteQueuePositions.Enqueue(pos);
            pendingDeletion.Add(pos);
        }
    }

    public Vector3Int lastPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    public BoundsInt GetCameraWorldBounds()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No main camera found for TileInfiniteCameraSpawner");
            return new BoundsInt(Vector3Int.zero, new Vector3Int(1, 2, 1));
        }
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        Vector3 camPos = cam.transform.position;
        int minX = Mathf.FloorToInt(camPos.x - camWidth / 2f);
        int maxX = Mathf.CeilToInt(camPos.x + camWidth / 2f);
        int minY = Mathf.FloorToInt(camPos.y - camHeight / 3f);
        int maxY = Mathf.CeilToInt(camPos.y + camHeight / 3f);
        int minZ = Mathf.FloorToInt(playerTransform != null ? playerTransform.position.z : 0);
        int sizeZ = 1;
        return new BoundsInt(minX, minY, minZ, maxX - minX + 1, maxY - minY + 1, sizeZ);
    }

    public bool IsInPlayerZSafeRange(Vector3Int pos)
    {
        if (playerTransform == null) return false;
        int playerZ = Mathf.RoundToInt(playerTransform.position.z);
        return pos.z >= playerZ - 2 && pos.z <= playerZ + 2;
    }

    public void ClearAllTilemapsAtPosition(Vector3Int pos)
    {
        if (pendingDeletion.Contains(pos)) return;
        foreach (var spacing in tilemapZSpacings)
        {
            if (spacing.tilemap != null)
                spacing.tilemap.SetTile(pos, null);
        }
    }

    void Awake()
    {
        if (seedSelector == null)
            seedSelector = GetComponent<SeedSelector>();

        if (worldSeedApplier == null)
            worldSeedApplier = GetComponent<WorldSeedApplier>();
        if (worldSeedApplier == null)
            worldSeedApplier = gameObject.AddComponent<WorldSeedApplier>();

        if (worldArchiveManager == null)
            worldArchiveManager = GetComponent<WorldArchiveManager>();
        if (worldArchiveManager == null)
            worldArchiveManager = gameObject.AddComponent<WorldArchiveManager>();

        worldSeedApplier.ApplySeed(this, seedSelector);
        biomeRand = new System.Random(seedSelector.usedSeedInt);

        worldArchiveManager.Init(seedSelector.usedSeedString, enableWorldArchive);

        if (chunkUpdateManager == null)
        {
            chunkUpdateManager = GetComponent<ChunkUpdateManager>();
            if (chunkUpdateManager == null)
                chunkUpdateManager = gameObject.AddComponent<ChunkUpdateManager>();
            chunkUpdateManager.spawner = this;
        }
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

        if (tile == null)
        {
            var tileData = worldArchiveManager.TryGetTile(cell);
            if (tileData != null && biomeManager != null)
            {
                int biomeIndex = GetChunkBiome(cell.x / ChunkSize, cell.z);
                tile = biomeManager.ResolveTileFromTag(biomeIndex, tileData.blockTagOrName);
            }
        }
        return tile;
    }

    public void BatchDrawFromArchive_Defensive(Vector3Int pos, int z, int playerZ, int biomeIndex)
    {
        var tileData = worldArchiveManager.TryGetTile(pos);
        if (tileData == null) return;
        TileBase resolvedTile = (biomeManager != null)
            ? biomeManager.ResolveTileFromTag(biomeIndex, tileData.blockTagOrName)
            : null;

        ClearAllTilemapsAtPosition(pos);

        for (int i = 0; i < tilemapZSpacings.Count; i++)
        {
            var spacing = tilemapZSpacings[i];
            if (spacing.tilemap != null && z == playerZ + (int)spacing.zSpacing)
            {
                SetTileDefensively(spacing.tilemap, pos, resolvedTile);
                break;
            }
        }
    }

    public void SetTileDefensivelyWithDeleteQueue(Tilemap tilemap, Vector3Int pos, TileBase tile)
    {
        QueueTileForDeleteQueueTilemap(pos);
        SetTileDefensively(tilemap, pos, tile);
    }

    public void SetTileDefensively(Tilemap tilemap, Vector3Int pos, TileBase tile)
    {
        if (pendingDeletion.Contains(pos)) return;
        if (chunkUpdateManager != null)
            chunkUpdateManager.QueueTileForDeletion(tilemap, pos);
        tilemap.SetTile(pos, tile);
    }

    public int GetChunkBuffer(int chunkX, int z)
    {
        int hash = seedSelector.usedSeedInt ^ (chunkX * 73856093) ^ (z * 19349663);
        System.Random rand = new System.Random(hash);
        return rand.Next(3, 11);
    }

    public int GetChunkBiome(int chunkX, int z)
    {
        Vector2Int key = new Vector2Int(chunkX, z);
        if (!chunkBiomes.TryGetValue(key, out int biomeIndex))
        {
            int biomeCount = (biomeManager != null && biomeManager.biomes != null) ? biomeManager.biomes.Count : 0;
            if (biomeCount == 0) return -1;
            biomeIndex = biomeRand.Next(0, biomeCount);
            chunkBiomes[key] = biomeIndex;
        }
        return biomeIndex;
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

    public void SetTileForZ(Vector3Int pos, int z, int playerZ, TileBase tile)
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

    public void ArchiveTileIfNeeded(
      Vector3Int pos,
      int x,
      int y,
      int z,
      int biomeIndex,
      bool isSurface,
      int chunkX,
      Vector3Int playerPos,
      float halfWidth,
      float discoveryRadius
    )
    {
        worldArchiveManager.ArchiveTileIfNeeded(
            pos, x, y, z, biomeIndex, isSurface, chunkX, playerPos, halfWidth, discoveryRadius,
            caveUtility,
            (a, b, c) => GetSurfaceY(a, c),
            (a, b, c) => GetChunkBuffer(a, b),
            (biomeIdx) =>
            {
                if (biomeManager != null && biomeManager.biomes != null &&
                    biomeIdx >= 0 && biomeIdx < biomeManager.biomes.Count)
                    return biomeManager.biomes[biomeIdx].name.Trim().ToLower();
                return "untagged";
            },
            worldBottomY
        );
    }

    public void SpawnOrLoadChunk_Defensive(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render)
    {
        int biomeIndex = GetChunkBiome(chunkX, z);
        int startX = chunkX * ChunkSize;
        int endX = startX + ChunkSize - 1;
        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);
        float halfWidth = 10f;
        float caveDiscoveryRadius = 10f;

        for (int x = startX; x <= endX; x++)
        {
            float hillValue = GetHillValue(x, z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            if (render && !deletedTiles.Contains(surfacePos) && ShouldSpawnTile(surfacePos, playerCell))
            {
                ArchiveTileIfNeeded(surfacePos, x, surfaceY, z, biomeIndex, true, chunkX, playerCell, halfWidth, caveDiscoveryRadius);
                activeTiles.Add(surfacePos);
            }
            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                if (render && !deletedTiles.Contains(dirtPos) && ShouldSpawnTile(dirtPos, playerCell))
                {
                    ArchiveTileIfNeeded(dirtPos, x, y, z, biomeIndex, false, chunkX, playerCell, halfWidth, caveDiscoveryRadius);
                    activeTiles.Add(dirtPos);
                }
            }
        }

        for (int x = startX; x <= endX; x++)
        {
            float hillValue = GetHillValue(x, z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            if (render && !deletedTiles.Contains(surfacePos) && ShouldSpawnTile(surfacePos, playerCell))
                BatchDrawFromArchive_Defensive(surfacePos, z, playerZ, biomeIndex);

            for (int y = surfaceY - 1; y >= buildBottom; y--)
            {
                Vector3Int dirtPos = new Vector3Int(x, y, z);
                if (render && !deletedTiles.Contains(dirtPos) && ShouldSpawnTile(dirtPos, playerCell))
                    BatchDrawFromArchive_Defensive(dirtPos, z, playerZ, biomeIndex);
            }
        }

        DeleteTilesAbovePlayer(45);
    }

    public Vector3Int lastTriggeredPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    public void UpdateWorldIfNeeded(Vector3Int playerPos)
    {
        Vector3 playerWorldPos = playerTransform.position;
        Vector3Int playerCell = groundTilemap.WorldToCell(playerWorldPos);
        int playerZ = playerCell.z;
        int playerY = playerCell.y;
        lastPlayerPosForQuit = playerCell;

        float halfWidth = 10f;
        int minX = playerCell.x - (int)halfWidth - buffer;
        int maxX = playerCell.x + (int)halfWidth + buffer;
        float halfCamHeight = 7f;
        int minY = playerY - (int)halfCamHeight - buffer;
        int maxY = playerY + (int)halfCamHeight + buffer;
        int buildBottom = Mathf.Min(minY, playerY, buildDepthBelowPlayer);

        int playerChunk = Mathf.FloorToInt((float)playerCell.x / ChunkSize);
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

        BoundsInt camBounds = GetCameraWorldBounds();
        int camBuffer = 3;
        for (int z_i = 0; z_i < zs.Length; z_i++)
        {
            int z = zs[z_i];
            for (int chunkX = chunkGenLeft; chunkX <= chunkGenRight; chunkX++)
            {
                int startX = chunkX * ChunkSize;
                int endX = startX + ChunkSize - 1;
                bool inCamera = endX >= (camBounds.xMin - camBuffer) && startX <= (camBounds.xMax + camBuffer)
                                && z >= (camBounds.z - camBuffer) && z <= (camBounds.z + camBuffer);
                bool render = chunkX >= chunkRenderLeft && chunkX <= chunkRenderRight;
                if (inCamera)
                {
                    SpawnOrLoadChunk_Defensive(chunkX, z, buildBottom, maxY, playerZ, render);
                }
                else if (chunkUpdateManager != null)
                {
                    chunkUpdateManager.RequestChunk(chunkX, z, buildBottom, maxY, playerZ, render,
                        new Vector3((startX + endX) * 0.5f, (buildBottom + maxY) * 0.5f, z));
                }
            }
        }

        if (caveUtility != null && playerTransform != null)
        {
            playerZ = Mathf.RoundToInt(playerTransform.position.z);
            if (worldArchiveManager.worldArchive != null)
                caveUtility.DrawCavesFromArchive(worldArchiveManager.worldArchive, playerZ);
        }

        UpdateHillCurvePreview(playerZ);
    }

    public bool ShouldSpawnTile(Vector3Int pos, Vector3Int playerPos, int maxAbovePlayer = 100)
    {
        return pos.y <= playerPos.y + maxAbovePlayer;
    }

    public void DeleteTilesAbovePlayer(int maxAbovePlayer = 45)
    {
        if (groundTilemap == null || playerTransform == null) return;
        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);
        var tilesToDelete = new List<Vector3Int>();
        foreach (var tile in activeTiles)
        {
            if (tile.y > playerCell.y + maxAbovePlayer)
            {
                tilesToDelete.Add(tile);
            }
        }
        foreach (var pos in tilesToDelete)
        {
            worldArchiveManager.DeleteTile(pos, groundTilemap, deletedTiles);
            activeTiles.Remove(pos);
        }
    }

    public void UpdateHillCurvePreview(int zLayer)
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

    public bool IsChunkInCamera(int chunkX, int z, int playerZ, int margin = 2)
    {
        BoundsInt camBounds = GetCameraWorldBounds();
        int startX = chunkX * ChunkSize;
        int endX = startX + ChunkSize - 1;
        bool inX = endX >= (camBounds.xMin - margin) && startX <= (camBounds.xMax + margin);
        bool inZ = z >= (camBounds.z - margin) && z <= (camBounds.z + margin);
        return inX && inZ;
    }
}