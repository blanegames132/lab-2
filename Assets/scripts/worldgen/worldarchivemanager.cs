using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WorldArchiveManager : MonoBehaviour
{
    [Header("Archiving")]
    public bool enableWorldArchive = true;
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private string worldSeed = "defaultSeed";

    [Header("Camera Reference")]
    public Camera mainCamera;
    public Tilemap referenceTilemap;
    [Min(1)] public int cameraArchiveBuffer = 8;

    [Header("Tilemaps to Save On Quit")]
    public List<Tilemap> tilemapsToSaveOnQuit = new List<Tilemap>();

    [Header("Performance Tuning")]
    public int maxArchiveActionsPerFrame = 20;

    [Header("Debug/Safety")]
    public int maxArchiveQueueSize = 10000;

    // Internal runtime fields (not shown in Inspector)
    public ChunkedWorldArchive worldArchive;
    private HashSet<Vector3Int> deletedTiles = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileData> pendingTiles = new Dictionary<Vector3Int, TileData>();
    private Queue<System.Action> archiveQueue = new Queue<System.Action>();
    private Coroutine archiveThrottleCoroutine = null;
    private bool archivingStarted = false;

    // NEW: Keep track of chunks that need to be saved due to deletions
    private HashSet<(int x, int z)> chunksNeedingSave = new HashSet<(int x, int z)>();

#if UNITY_EDITOR
    private void EditorEnable()
    {
        EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
    }
    private void EditorDisable()
    {
        EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
    }
    private void OnEditorPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            ArchiveAllTilesOnQuit();
            Debug.Log("WorldArchiveManager: Game stopped in Editor. ArchiveAllTilesOnQuit called.");
        }
    }
#endif

    private void Awake()
    {
        worldArchive = new ChunkedWorldArchive(worldSeed, chunkSize);
    }

    private void OnEnable()
    {
        TileEventBus.ShouldBlockTile += ShouldBlockTile;
        TileEventBus.OnTileDelete += HandleTileDelete;
#if UNITY_EDITOR
        EditorEnable();
#endif
    }
    private void OnDisable()
    {
        TileEventBus.ShouldBlockTile -= ShouldBlockTile;
        TileEventBus.OnTileDelete -= HandleTileDelete;
#if UNITY_EDITOR
        EditorDisable();
#endif
    }

    private void Start()
    {
        if (enableWorldArchive && worldArchive != null)
        {
            CleanOrphanTiles();
        }
    }

    private void CleanOrphanTiles()
    {
        int orphanCount = 0;
        foreach (var tilemap in tilemapsToSaveOnQuit)
        {
            if (tilemap == null) continue;
            foreach (var cellPos in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cellPos))
                {
                    var archiveData = worldArchive.TryGetTile(cellPos);
                    if (archiveData == null)
                    {
                        tilemap.SetTile(cellPos, null);
                        orphanCount++;
                    }
                }
            }
        }
        if (orphanCount > 0)
            Debug.Log($"WorldArchiveManager: Removed {orphanCount} orphan tiles from scene at game start.");
    }

    // This is hooked up to TileEventBus so the spawner will never place deleted tiles.
    public bool ShouldBlockTile(Vector3Int pos)
    {
        return enableWorldArchive && deletedTiles.Contains(pos);
    }

    public void OnTileSet(Tilemap tilemap, Vector3Int pos, TileBase tile)
    {
        if (!archivingStarted || !enableWorldArchive) return;
        if (IsInCameraArea(pos))
        {
            if (archiveQueue.Count < maxArchiveQueueSize)
            {
                archiveQueue.Enqueue(() =>
                {
                    pendingTiles[pos] = new TileData { blockTagOrName = tile != null ? tile.name : "air" };
                    deletedTiles.Remove(pos); // If respawned by user, unmark as deleted
                    var chunkCoord = WorldToChunk(pos);
                    chunksNeedingSave.Remove(chunkCoord); // If tile is placed, remove from needing save
                });
            }
            else
            {
                Debug.LogWarning($"WorldArchiveManager: Archive queue overflow! Dropping archive for tile {pos}");
            }
            TryStartThrottle();
        }
    }

    /// <summary>
    /// Called when a tile is deleted (queued, not instant)
    /// </summary>
    public void OnTileDelete(Tilemap tilemap, Vector3Int pos)
    {
        if (!archivingStarted && enableWorldArchive)
        {
            archivingStarted = true;
            Debug.Log("WorldArchiveManager: Archiving started after first block deletion.");
        }
        if (!archivingStarted || !enableWorldArchive) return;
        if (IsInCameraArea(pos))
        {
            if (archiveQueue.Count < maxArchiveQueueSize)
            {
                archiveQueue.Enqueue(() =>
                {
                    pendingTiles[pos] = new TileData { blockTagOrName = "air" };
                    deletedTiles.Add(pos);
                    var chunkCoord = WorldToChunk(pos);
                    chunksNeedingSave.Add(chunkCoord);
                    if (tilemap != null)
                        tilemap.SetTile(pos, null);
                    // INSTANT SAVE for this chunk if desired
                    worldArchive.SaveChunks(new HashSet<(int x, int z)> { chunkCoord });
                    Debug.Log($"WorldArchiveManager: Instantly saved chunk {chunkCoord} due to deletion at {pos}.");
                });
            }
            else
            {
                Debug.LogWarning($"WorldArchiveManager: Archive queue overflow! Dropping archive for tile {pos}");
            }
            TryStartThrottle();
        }
    }

    /// <summary>
    /// Called instantly when event bus triggers a tile delete (not queued)
    /// </summary>
    private void HandleTileDelete(Tilemap tilemap, Vector3Int pos)
    {
        if (enableWorldArchive && worldArchive != null)
        {
            worldArchive.RemoveTile(pos);
            deletedTiles.Add(pos);
            var chunkCoord = WorldToChunk(pos);
            chunksNeedingSave.Add(chunkCoord);
            pendingTiles.Remove(pos);
            if (tilemap != null)
                tilemap.SetTile(pos, null);

            // INSTANT SAVE for this chunk (so it's always up to date after click)
            worldArchive.SaveChunks(new HashSet<(int x, int z)> { chunkCoord });
            Debug.Log($"WorldArchiveManager: Instantly saved chunk {chunkCoord} after deletion at {pos}.");
        }
    }

    private (int x, int z) WorldToChunk(Vector3Int pos)
    {
        return (Mathf.FloorToInt((float)pos.x / chunkSize),
                Mathf.FloorToInt((float)pos.z / chunkSize));
    }

    private void TryStartThrottle()
    {
        if (archiveThrottleCoroutine == null && archiveQueue.Count > 0)
            archiveThrottleCoroutine = StartCoroutine(ArchiveThrottleRoutine());
    }

    private System.Collections.IEnumerator ArchiveThrottleRoutine()
    {
        while (archiveQueue.Count > 0)
        {
            int actionsThisFrame = 0;
            while (actionsThisFrame < maxArchiveActionsPerFrame && archiveQueue.Count > 0)
            {
                var action = archiveQueue.Dequeue();
                action.Invoke();
                actionsThisFrame++;
            }
            if (actionsThisFrame > 0)
                Debug.Log($"WorldArchiveManager: Processed {actionsThisFrame} archive actions this frame. {archiveQueue.Count} remain.");
            yield return null;
        }
        archiveThrottleCoroutine = null;
    }

    public bool IsInCameraArea(Vector3Int cell)
    {
        if (mainCamera == null || referenceTilemap == null) return true;
        Vector3 camMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
        Vector3 camMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, mainCamera.nearClipPlane));
        Vector3Int minCell = referenceTilemap.WorldToCell(camMin);
        Vector3Int maxCell = referenceTilemap.WorldToCell(camMax);
        minCell.x -= cameraArchiveBuffer;
        minCell.y -= cameraArchiveBuffer;
        maxCell.x += cameraArchiveBuffer;
        maxCell.y += cameraArchiveBuffer;
        return cell.x >= minCell.x && cell.x <= maxCell.x &&
               cell.y >= minCell.y && cell.y <= maxCell.y;
    }

    /// <summary>
    /// Archives ALL existing tiles in all tilemaps in tilemapsToSaveOnQuit and saves to disk.
    /// This is called on quit or when play mode is stopped.
    /// </summary>
    public void ArchiveAllTilesOnQuit()
    {
        if (!enableWorldArchive || worldArchive == null)
        {
            Debug.LogWarning("WorldArchiveManager: Archiving disabled or archive not present.");
            return;
        }

        Debug.Log("WorldArchiveManager: Archiving ALL tiles in scene to disk...");
        int archivedCount = 0;
        foreach (var tilemap in tilemapsToSaveOnQuit)
        {
            if (tilemap == null) continue;
            foreach (var cellPos in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(cellPos))
                {
                    var tile = tilemap.GetTile(cellPos);
                    worldArchive.SetTile(cellPos, new TileData { blockTagOrName = tile != null ? tile.name : "air" });
                    archivedCount++;
                }
            }
        }

        // Save ALL chunks (as you requested)
        worldArchive.SaveAll();

        Debug.Log($"WorldArchiveManager: Finished archiving. Saved {archivedCount} tiles to disk.");
    }

    private void OnApplicationQuit()
    {
        ArchiveAllTilesOnQuit();
        Debug.Log("WorldArchiveManager: Game stopped or quit. ArchiveAllTilesOnQuit called.");
    }
}