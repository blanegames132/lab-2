using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Requests chunk spawns from spawner. If farther from player, requests far chunk (draw 100x slower).
/// Toggle enableChunkManager in spawner to activate/deactivate.
/// </summary>
public class ModularChunkManager : MonoBehaviour
{
    public InfiniteCameraSpawnerModular spawner;
    public int chunkSize = 16;
    public int chunkRadius = 3; // Fast area
    public int bufferRadius = 6; // Buffer/slow area
    public bool drawGizmos = true;
    public Color loadedColor = Color.green, bufferColor = Color.cyan;

    private HashSet<Vector2Int> loadedChunks = new HashSet<Vector2Int>();
    private Vector2Int lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);

    void Update()
    {
        if (spawner == null || spawner.playerTransform == null || !spawner.enableChunkManager) return;

        Vector3 playerWorld = spawner.playerTransform.position;
        Vector2Int playerChunk = new Vector2Int(
            Mathf.FloorToInt(playerWorld.x / chunkSize),
            Mathf.FloorToInt(playerWorld.y / chunkSize)
        );

        // Only trigger update if player enters a new chunk
        if (playerChunk != lastPlayerChunk)
        {
            lastPlayerChunk = playerChunk;
            HashSet<Vector2Int> neededChunks = new HashSet<Vector2Int>();
            HashSet<Vector2Int> bufferChunks = new HashSet<Vector2Int>();

            for (int dx = -bufferRadius; dx <= bufferRadius; dx++)
            {
                for (int dy = -bufferRadius; dy <= bufferRadius; dy++)
                {
                    int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    var coord = new Vector2Int(playerChunk.x + dx, playerChunk.y + dy);
                    if (dist <= chunkRadius)
                        neededChunks.Add(coord);
                    else if (dist <= bufferRadius)
                        bufferChunks.Add(coord);
                }
            }

            // Request spawner to spawn all needed chunks
            foreach (var coord in neededChunks)
            {
                if (!loadedChunks.Contains(coord))
                {
                    loadedChunks.Add(coord);
                    spawner.RequestChunkSpawn(coord.x, coord.y, chunkSize, false); // Fast
                }
            }
            foreach (var coord in bufferChunks)
            {
                if (!loadedChunks.Contains(coord))
                {
                    loadedChunks.Add(coord);
                    spawner.RequestChunkSpawn(coord.x, coord.y, chunkSize, true); // Slow
                }
            }

            // Despawn chunks outside area
            var toRemove = new List<Vector2Int>();
            foreach (var coord in loadedChunks)
            {
                if (!neededChunks.Contains(coord) && !bufferChunks.Contains(coord))
                {
                    toRemove.Add(coord);
                    TileEventBus.BroadcastChunkDespawnRequest(coord.x, coord.y, chunkSize);
                }
            }
            foreach (var coord in toRemove)
                loadedChunks.Remove(coord);
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || spawner == null) return;
        Gizmos.color = loadedColor;
        foreach (var coord in loadedChunks)
        {
            Gizmos.DrawWireCube(new Vector3(coord.x * chunkSize + chunkSize / 2f, coord.y * chunkSize + chunkSize / 2f, 0), new Vector3(chunkSize, chunkSize, 0.5f));
        }
    }
}