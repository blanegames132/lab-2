using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles all chunk update scheduling, throttling, and world/cave archive logic.
/// Chunks in camera range update instantly; buffer chunks update slower; distant chunks are deleted.
/// Also triggers world/cave/archiving on player movement.
/// </summary>
public class ChunkUpdateManager : MonoBehaviour
{
    [Header("References")]
    public TileInfiniteCameraSpawner spawner;

    private Vector3Int lastTriggeredPlayerCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    [Header("Chunk Processing Settings")]
    [Min(1)] public int maxChunksPerFrame = 20;
    [Min(1)] public int maxBufferChunksPerFrame = 1;
    [Min(1f)] public float updateDistance = 64f;
    [Min(1f)] public float bufferDistance = 96f;
    [Min(1f)] public float tilemapActiveRange = 160f;
    [Min(1f)] public float chunkDeleteDistance = 192f;
    [Min(1)] public int bufferProcessFrameSkip = 100;
    [Min(1)] public int deleteProcessFrameSkip = 15;

    private readonly Queue<ChunkRequest> inCameraQueue = new();
    private readonly Queue<ChunkRequest> bufferQueue = new();
    private readonly Queue<ChunkRequest> deleteQueue = new();
    private int bufferFrameCounter;
    private int deleteFrameCounter;

    // Defensive tile deletion queue
    private Queue<(Tilemap tilemap, Vector3Int pos)> tilesToDelete = new();
    private int lastPlayerZ = int.MinValue;

    void Update()
    {
        if (spawner?.playerTransform == null) return;

        Vector3 playerPos = spawner.playerTransform.position;

        // Defensive: On player Z move, queue all tiles for deletion
        int playerZ = Mathf.RoundToInt(playerPos.z);
        if (playerZ != lastPlayerZ)
        {
            lastPlayerZ = playerZ;
            QueueAllTilesForDeletionOnZMove();
        }
        // Defensive: Process tile deletion queue each frame
        ProcessTileDeletionQueue(100);

        // 1. In-camera chunks: instant (100000000000000000000000000000000000X faster: always process all immediately)
        ProcessQueue(inCameraQueue, int.MaxValue);

        // 2. Buffer chunks: slow
        bufferFrameCounter++;
        if (bufferFrameCounter >= bufferProcessFrameSkip)
        {
            bufferFrameCounter = 0;
            ProcessQueue(bufferQueue, maxBufferChunksPerFrame);
        }

        // 3. Deletion: slow
        deleteFrameCounter++;
        if (deleteFrameCounter >= deleteProcessFrameSkip)
        {
            deleteFrameCounter = 0;
            ProcessDeleteQueue(playerPos);
        }

        // 4. World/cave/archive update on player cell change (all in one place)
        if (spawner.groundTilemap != null && spawner.frontTilemap != null &&
            spawner.middleFrontTilemap != null && spawner.middleBackTilemap != null &&
            spawner.backTilemap != null)
        {
            Vector3Int playerCell = Vector3Int.FloorToInt(spawner.playerTransform.position);

            if (playerCell != lastTriggeredPlayerCell)
            {
                lastTriggeredPlayerCell = playerCell;

                // Update world for new player cell
                spawner.UpdateWorldIfNeeded(playerCell);

                // Mark caves as discovered and unload distant archive chunks
                if (spawner.worldArchive != null)
                {
                    ChunkUpdateManager.MarkCavesDiscoveredAroundPlayer(spawner.worldArchive, playerCell);
                    spawner.worldArchive.UnloadDistantChunks(playerCell);
                }
            }
        }

        // 5. Cull off-camera chunk GameObjects (render culling)
        if (spawner != null && spawner.playerTransform != null)
        {
            spawner.CullOffCameraChunks(Mathf.RoundToInt(spawner.playerTransform.position.z));
        }
    }

    public static void MarkCavesDiscoveredAroundPlayer(ChunkedWorldArchive archive, Vector3 playerPosition, float radius = 4f)
    {
        if (archive == null) return;
        bool changed = false;
        foreach (var pair in archive.AllTiles())
        {
            Vector3Int pos = pair.Key;
            TileData tileData = pair.Value;
            if (tileData.blockTagOrName == "cave")
            {
                float dist = Vector3.Distance(playerPosition, pos);
                if (dist <= radius && !tileData.discovered)
                {
                    tileData.discovered = true;
                    archive.SetTile(pos, tileData);
                    changed = true;
                    Debug.Log($"Discovered cave at {pos} (distance {dist:F2})");
                }
            }
        }
        if (changed)
            archive.SaveAll();
    }

    /// <summary>
    /// Enqueue a chunk update request.
    /// </summary>
    public void RequestChunk(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render, Vector3 centerWorldPos)
    {
        if (spawner?.playerTransform == null)
        {
            inCameraQueue.Enqueue(new ChunkRequest(chunkX, z, buildBottom, maxY, playerZ, render, centerWorldPos));
            return;
        }

        float dist = Vector3.Distance(spawner.playerTransform.position, centerWorldPos);
        bool isInCamera = false;

        // Camera bounds optimization: if the chunk is within any active tilemap's camera bounds, treat as in-camera
        foreach (var spacing in spawner.tilemapZSpacings)
        {
            var tilemap = spacing.tilemap;
            if (tilemap == null) continue;
            BoundsInt bounds = tilemap.cellBounds;
            // If the chunk center is in any tilemap's bounds, treat as in-camera (max speed)
            if (bounds.Contains(Vector3Int.FloorToInt(centerWorldPos)))
            {
                isInCamera = true;
                break;
            }
        }

        if (isInCamera || dist < updateDistance)
        {
            // INSTANT CHUNK: process immediately, do not enqueue
            spawner.SpawnOrLoadChunk_Defensive(chunkX, z, buildBottom, maxY, playerZ, render);
        }
        else if (dist < bufferDistance)
        {
            bufferQueue.Enqueue(new ChunkRequest(chunkX, z, buildBottom, maxY, playerZ, render, centerWorldPos));
        }
        else if (dist > chunkDeleteDistance)
        {
            deleteQueue.Enqueue(new ChunkRequest(chunkX, z, buildBottom, maxY, playerZ, render, centerWorldPos));
        }
        // If outside tilemapActiveRange, don't spawn or process
    }

    private void ProcessQueue(Queue<ChunkRequest> queue, int maxCount)
    {
        int count = Mathf.Min(maxCount, queue.Count);
        for (int i = 0; i < count; i++)
        {
            var req = queue.Dequeue();
            spawner.SpawnOrLoadChunk_Defensive(req.chunkX, req.z, req.buildBottom, req.maxY, req.playerZ, req.render);
        }
    }

    private void ProcessDeleteQueue(Vector3 playerPos)
    {
        int count = deleteQueue.Count;
        for (int i = 0; i < count; i++)
        {
            var req = deleteQueue.Dequeue();
            float dist = Vector3.Distance(playerPos, req.centerWorldPos);
            if (dist > chunkDeleteDistance)
            {
                spawner.RemoveChunk(req.chunkX, req.z);
            }
        }
    }

    // --- Defensive Deletion Methods ---

    /// <summary>
    /// Queue all tiles in all layers for deletion (on Z move, or anytime).
    /// Never enqueue tiles in player's Z-safe range!
    /// </summary>
    private void QueueAllTilesForDeletionOnZMove()
    {
        if (spawner == null || spawner.tilemapZSpacings == null) return;
        foreach (var tmSpacing in spawner.tilemapZSpacings)
        {
            Tilemap tilemap = tmSpacing.tilemap;
            if (tilemap == null) continue;
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos) && !spawner.IsInPlayerZSafeRange(pos))
                    tilesToDelete.Enqueue((tilemap, pos));
            }
        }
    }

    /// <summary>
    /// Process a limited number of tile deletions per frame for smooth performance.
    /// Only deletes tiles that are outside player's Z-safe range (all tiles in queue are already checked).
    /// </summary>
    private void ProcessTileDeletionQueue(int maxPerFrame = 100)
    {
        int count = Mathf.Min(maxPerFrame, tilesToDelete.Count);
        for (int i = 0; i < count; i++)
        {
            var (tilemap, pos) = tilesToDelete.Dequeue();
            // No need to check IsInPlayerZSafeRange(pos), already filtered during enqueue!
            tilemap.SetTile(pos, null);
        }
    }

    /// <summary>
    /// Queue a single tile for deletion (defensive). Never enqueue if in player's Z-safe range.
    /// </summary>
    public void QueueTileForDeletion(Tilemap tilemap, Vector3Int pos)
    {
        if (tilemap != null && tilemap.HasTile(pos) && !spawner.IsInPlayerZSafeRange(pos))
            tilesToDelete.Enqueue((tilemap, pos));
    }

    /// <summary>
    /// Defensive tile set: queue for deletion, then set.
    /// </summary>
    public void SetTileDefensively(Tilemap tilemap, Vector3Int pos, TileBase tile)
    {
        QueueTileForDeletion(tilemap, pos);
        tilemap.SetTile(pos, tile);
    }

    private readonly struct ChunkRequest
    {
        public readonly int chunkX, z, buildBottom, maxY, playerZ;
        public readonly bool render;
        public readonly Vector3 centerWorldPos;

        public ChunkRequest(int chunkX, int z, int buildBottom, int maxY, int playerZ, bool render, Vector3 centerWorldPos)
        {
            this.chunkX = chunkX;
            this.z = z;
            this.buildBottom = buildBottom;
            this.maxY = maxY;
            this.playerZ = playerZ;
            this.render = render;
            this.centerWorldPos = centerWorldPos;
        }
    }
}