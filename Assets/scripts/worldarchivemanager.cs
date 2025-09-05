using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class WorldArchiveManager : MonoBehaviour
{
    public bool enableWorldArchive = true;
    public ChunkedWorldArchive worldArchive { get; private set; }
    [SerializeField] public int chunkSize = 16;
    [SerializeField] public BiomeManager biomeManager;

    private Dictionary<Vector3Int, TileData> pendingTiles = new Dictionary<Vector3Int, TileData>();
    [SerializeField, Tooltip("Number of tiles currently queued for archiving (read-only)")]
    private int queuedTileCount;

    private Queue<Action> archiveQueue = new Queue<Action>();
    private Coroutine archiveTrottleCoroutine = null;

    // Track last frame's movement key state
    private bool wasMovementKeyPressed = false;

    // Track chunks with new tiles added in this session before flush
    private HashSet<(int x, int z)> affectedChunks = new HashSet<(int x, int z)>();

    public void Init(string seedString, bool enableArchive)
    {
        enableWorldArchive = enableArchive;
        if (enableWorldArchive)
            worldArchive = new ChunkedWorldArchive(seedString, chunkSize);
        else
            worldArchive = null;
    }

    public void ArchiveTile(Vector3Int pos, string tag)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            archiveQueue.Enqueue(() =>
            {
                pendingTiles[pos] = new TileData { blockTagOrName = tag };
                affectedChunks.Add(WorldToChunk(pos));
                UpdateInspectorCount();
            });
            TryStartTrottle();
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
        float discoveryRadius,
        TileCaveUtility caveUtility,
        Func<int, int, int, int> getSurfaceY,
        Func<int, int, int, int> getChunkBuffer,
        Func<int, string> getBiomeTag,
        int worldBottomY
    )
    {
        if (!(enableWorldArchive && worldArchive != null)) return;
        var existing = worldArchive.TryGetTile(pos);
        if (existing != null) return;

        int surfaceY = getSurfaceY(x, y, z);
        bool isCave = caveUtility != null && caveUtility.IsCaveAt(x, y, z, surfaceY);

        string tag = null;

        if (isCave)
            tag = "cave";
        else if (y == surfaceY)
            tag = "surface:" + getBiomeTag(biomeIndex);
        else if (y < surfaceY && y >= surfaceY - 1)
            tag = "subsurface:" + getBiomeTag(biomeIndex);
        else if (y < surfaceY - 1 && y > worldBottomY)
            tag = "ground:" + getBiomeTag(biomeIndex);
        else if (y == worldBottomY)
            tag = "bedrock:" + getBiomeTag(biomeIndex);
        else
            tag = "air";

        if (string.IsNullOrEmpty(tag))
            tag = (UnityEngine.Random.value > 0.5f) ? "unt1" : "unt2";

        bool shouldDiscover = false;
        if (tag == "cave" && Vector3.Distance(playerPos, pos) <= discoveryRadius)
            shouldDiscover = true;

        archiveQueue.Enqueue(() =>
        {
            pendingTiles[pos] = new TileData { blockTagOrName = tag, discovered = shouldDiscover };
            affectedChunks.Add(WorldToChunk(pos));
            UpdateInspectorCount();
        });
        TryStartTrottle();
    }

    private (int x, int z) WorldToChunk(Vector3Int pos)
    {
        return (Mathf.FloorToInt((float)pos.x / chunkSize),
                Mathf.FloorToInt((float)pos.z / chunkSize));
    }

    private void TryStartTrottle()
    {
        if (archiveTrottleCoroutine == null && archiveQueue.Count > 0)
        {
            archiveTrottleCoroutine = StartCoroutine(ArchiveTrottleRoutine());
        }
    }

    private IEnumerator ArchiveTrottleRoutine()
    {
        while (archiveQueue.Count > 0)
        {
            var action = archiveQueue.Dequeue();
            action.Invoke();
            yield return null;
        }
        archiveTrottleCoroutine = null;
    }

    public string GetBiomeTag(int biomeIdx)
    {
        if (biomeManager != null && biomeManager.biomes != null &&
            biomeIdx >= 0 && biomeIdx < biomeManager.biomes.Count)
            return biomeManager.biomes[biomeIdx].name.Trim().ToLower();
        return "untagged";
    }

    public void DeleteTile(Vector3Int pos, Tilemap groundTilemap, HashSet<Vector3Int> deletedTiles)
    {
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);
        ArchiveTile(pos, "air");
        if (deletedTiles != null)
            deletedTiles.Add(pos);
    }

    public void DeleteTile(Vector3Int pos, Tilemap groundTilemap)
    {
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);
        ArchiveTile(pos, "air");
    }

    public void DeleteTile(Vector3Int pos)
    {
        ArchiveTile(pos, "air");
    }

    public TileData TryGetTile(Vector3Int pos)
    {
        if (enableWorldArchive && worldArchive != null)
            return worldArchive.TryGetTile(pos);
        return null;
    }

    public void SaveAll()
    {
        FlushArchiveToDisk();
    }

    void OnApplicationQuit()
    {
        FlushArchiveToDisk();
        if (enableWorldArchive && worldArchive != null)
        {
            worldArchive.SaveAll();
            Debug.Log("WorldArchiveManager: saved on exit.");
        }
    }

    void FlushArchiveToDisk()
    {
        if (pendingTiles.Count > 0 && enableWorldArchive && worldArchive != null)
        {
            // Only flush chunks that have new tiles
            var chunkTileMap = new Dictionary<(int x, int z), List<KeyValuePair<Vector3Int, TileData>>>();
            foreach (var kvp in pendingTiles)
            {
                var chunkCoord = WorldToChunk(kvp.Key);
                if (!chunkTileMap.ContainsKey(chunkCoord))
                    chunkTileMap[chunkCoord] = new List<KeyValuePair<Vector3Int, TileData>>();
                chunkTileMap[chunkCoord].Add(kvp);
            }

            foreach (var chunkCoord in chunkTileMap.Keys)
            {
                foreach (var kvp in chunkTileMap[chunkCoord])
                {
                    worldArchive.SetTile(kvp.Key, kvp.Value);
                }
            }
            worldArchive.SaveChunks(affectedChunks); // Save only affected chunks
            Debug.Log($"WorldArchiveManager: batch-saved {pendingTiles.Count} tiles to {affectedChunks.Count} affected chunks.");
            pendingTiles.Clear();
            affectedChunks.Clear();
            UpdateInspectorCount();
        }
    }

    void Update()
    {
        bool isMovementKeyPressed = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)
            || Input.GetKey(KeyCode.Space);

        // Only flush when movement keys transition from pressed -> not pressed
        if (!isMovementKeyPressed && wasMovementKeyPressed)
        {
            if (archiveTrottleCoroutine != null)
                StopCoroutine(archiveTrottleCoroutine);
            while (archiveQueue.Count > 0)
            {
                var action = archiveQueue.Dequeue();
                action.Invoke();
            }
            FlushArchiveToDisk();
            ShowAllTilesQueuedAndArchived();
        }

        wasMovementKeyPressed = isMovementKeyPressed;
    }

    private void UpdateInspectorCount()
    {
        queuedTileCount = pendingTiles.Count;
    }

    /// <summary>
    /// Shows all tiles queued and archived, including all tiles in the world and in each affected chunk.
    /// </summary>
    [ContextMenu("Show All Tiles Queued And Archived")]
    public void ShowAllTilesQueuedAndArchived()
    {
        // Show all currently queued (not flushed) tiles
        if (pendingTiles.Count > 0)
        {
            Debug.Log($"[QUEUED] Tiles currently queued in RAM ({pendingTiles.Count}):");
            foreach (var kvp in pendingTiles)
            {
                Vector3Int pos = kvp.Key;
                TileData data = kvp.Value;
                Debug.Log($"[QUEUED] Pos: {pos} | Tag: {data.blockTagOrName} | Discovered: {data.discovered}");
            }
        }
        else
        {
            Debug.Log("[QUEUED] No tiles are currently queued in RAM.");
        }

        // Show all archived tiles only from affected chunks
        if (worldArchive != null && affectedChunks.Count > 0)
        {
            foreach (var chunkCoord in affectedChunks)
            {
                var tiles = worldArchive.GetChunkTiles(chunkCoord.x, chunkCoord.z);
                Debug.Log($"[ARCHIVED] Tiles in chunk ({chunkCoord.x},{chunkCoord.z}):");
                foreach (var kvp in tiles)
                {
                    Vector3Int pos = kvp.Key;
                    TileData data = kvp.Value;
                    Debug.Log($"[ARCHIVED] Pos: {pos} | Tag: {data.blockTagOrName} | Discovered: {data.discovered}");
                }
            }
        }
        else if (worldArchive != null)
        {
            Debug.Log("[ARCHIVED] No affected chunks to show.");
        }
        else
        {
            Debug.Log("[ARCHIVED] No worldArchive instance available.");
        }

        // Show all archived tiles from all loaded chunks in the world archive
        if (worldArchive != null)
        {
            Debug.Log("[ARCHIVED] All tiles in worldArchive (all loaded chunks):");
            foreach (var pair in worldArchive.GetAllTiles()) // <--- FIXED!
            {
                Vector3Int pos = pair.Key;
                TileData data = pair.Value;
                Debug.Log($"[ARCHIVED] Pos: {pos} | Tag: {data.blockTagOrName} | Discovered: {data.discovered}");
            }
        }
    }
}