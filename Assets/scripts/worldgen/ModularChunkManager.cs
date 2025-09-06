using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chunk manager with bus decoupling. All green chunks are always spawned (all tiles in chunk).
/// Chunks adjacent to player spawn instantly, farther green chunks spawn slowly. Red chunks despawn immediately.
/// </summary>
public class ModularChunkManager : MonoBehaviour
{
    public int chunkSize = 16;
    public int chunkRadius = 3;
    public int bufferRadius = 6;
    public int despawnRadius = 9;
    public bool drawGizmos = true;
    public Transform playerTransform; // Assign in inspector!
    public Color loadedColor = Color.green, bufferColor = Color.cyan, despawnColor = Color.red, edgeGizmoColor = Color.yellow;
    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> bufferChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> redChunks = new HashSet<Vector2Int>();
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
    private Vector2Int minXCoord, maxXCoord;

    // Z offsets for layers (assign in inspector or awake)
    public int[] zOffsets = new int[] { 0, 1, 2, -1, -2 };

    private Queue<Vector2Int> slowSpawnQueue = new Queue<Vector2Int>();
    private int slowChunksPerFrame = 2; // How many slow green chunks to spawn per frame

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = FindObjectOfType<PlayerController>()?.transform; // Fallback
    }

    void Update()
    {
        if (playerTransform == null) return;
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(playerTransform.position.x / chunkSize),
            Mathf.FloorToInt(playerTransform.position.y / chunkSize)
        );

        // Only re-calculate zones when player enters a new chunk
        if (playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;

            // Calculate all zones
            HashSet<Vector2Int> newLoadedChunks = new HashSet<Vector2Int>();
            HashSet<Vector2Int> newBufferChunks = new HashSet<Vector2Int>();
            HashSet<Vector2Int> newRedChunks = new HashSet<Vector2Int>();

            for (int dx = -despawnRadius; dx <= despawnRadius; dx++)
            {
                for (int dy = -despawnRadius; dy <= despawnRadius; dy++)
                {
                    int dist = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                    var coord = new Vector2Int(playerChunk.x + dx, playerChunk.y + dy);

                    if (dist <= chunkRadius)
                        newLoadedChunks.Add(coord);
                    else if (dist <= bufferRadius)
                        newBufferChunks.Add(coord);
                    else if (dist <= despawnRadius)
                        newRedChunks.Add(coord);
                }
            }

            // Track furthest left/right X chunk for edge gizmo
            minXCoord = new Vector2Int(int.MaxValue, playerChunk.y);
            maxXCoord = new Vector2Int(int.MinValue, playerChunk.y);
            foreach (var coord in newLoadedChunks)
            {
                if (coord.x < minXCoord.x) minXCoord = coord;
                if (coord.x > maxXCoord.x) maxXCoord = coord;
            }

            // Defensive: spawn green chunks
            foreach (var coord in newLoadedChunks)
            {
                int distToPlayer = Mathf.Max(Mathf.Abs(coord.x - playerChunk.x), Mathf.Abs(coord.y - playerChunk.y));
                if (distToPlayer <= 1)
                {
                    if (!loadedChunks.Contains(coord))
                    {
                        loadedChunks.Add(coord);
                        ChunkManagerBus.RequestChunkSpawn(coord.x, coord.y, chunkSize, zOffsets);
                    }
                }
                else
                {
                    if (!loadedChunks.Contains(coord) && !slowSpawnQueue.Contains(coord))
                        slowSpawnQueue.Enqueue(coord);
                }
            }

            // Despawn/archive red chunks immediately
            foreach (var coord in new List<Vector2Int>(loadedChunks))
            {
                if (newRedChunks.Contains(coord))
                {
                    loadedChunks.Remove(coord);
                    ChunkManagerBus.RequestChunkDespawn(coord.x, coord.y, chunkSize, zOffsets);
                }
            }

            bufferChunks = newBufferChunks;
            redChunks = newRedChunks;
        }

        // Slow spawn: spawn a few green chunks per frame
        int spawnedThisFrame = 0;
        while (slowSpawnQueue.Count > 0 && spawnedThisFrame < slowChunksPerFrame)
        {
            var coord = slowSpawnQueue.Dequeue();
            if (!loadedChunks.Contains(coord))
            {
                loadedChunks.Add(coord);
                ChunkManagerBus.RequestChunkSpawn(coord.x, coord.y, chunkSize, zOffsets);
            }
            spawnedThisFrame++;
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = loadedColor;
        foreach (var coord in loadedChunks)
            Gizmos.DrawWireCube(new Vector3(coord.x * chunkSize + chunkSize / 2f, coord.y * chunkSize + chunkSize / 2f, 0), new Vector3(chunkSize, chunkSize, 0.5f));
        Gizmos.color = bufferColor;
        foreach (var coord in bufferChunks)
            Gizmos.DrawWireCube(new Vector3(coord.x * chunkSize + chunkSize / 2f, coord.y * chunkSize + chunkSize / 2f, 0), new Vector3(chunkSize, chunkSize, 0.5f));
        Gizmos.color = despawnColor;
        foreach (var coord in redChunks)
            Gizmos.DrawWireCube(new Vector3(coord.x * chunkSize + chunkSize / 2f, coord.y * chunkSize + chunkSize / 2f, 0), new Vector3(chunkSize, chunkSize, 0.5f));
        Gizmos.color = edgeGizmoColor;
        Gizmos.DrawCube(new Vector3(minXCoord.x * chunkSize + chunkSize / 2f, minXCoord.y * chunkSize + chunkSize / 2f, 1), new Vector3(chunkSize * 0.75f, chunkSize * 0.75f, 1f));
        Gizmos.DrawCube(new Vector3(maxXCoord.x * chunkSize + chunkSize / 2f, maxXCoord.y * chunkSize + chunkSize / 2f, 1), new Vector3(chunkSize * 0.75f, chunkSize * 0.75f, 1f));
    }
}