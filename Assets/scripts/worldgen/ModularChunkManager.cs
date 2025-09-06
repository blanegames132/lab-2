using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Modular Chunk Manager for InfiniteCameraSpawnerModular and similar systems.
/// Spawns/despawns chunks around player and works only with modular tilemap setups.
/// </summary>
public class ModularChunkManager : MonoBehaviour
{
    [Header("Modular Tilemaps (ground, front, middleFront, middleBack, back)")]
    public Tilemap[] tilemapLayers = new Tilemap[5];

    [Header("Chunk Settings")]
    public int chunkSize = 16;
    public int chunkSpawnDistance = 10;   // Chunks will be spawned within this distance from player (left/right)
    public int chunkDespawnDistance = 20; // Chunks further than this from player will be despawned

    [Header("Player Reference")]
    public Transform playerTransform;

    [Header("Show Chunk Gizmos")]
    public bool showChunkGizmos = true;
    public Color chunkGizmoColor = new Color(0.2f, 0.9f, 1f, 0.2f);

    private Dictionary<Vector3Int, Chunk> loadedChunks = new Dictionary<Vector3Int, Chunk>();

    void Update()
    {
        if (playerTransform == null || tilemapLayers == null || tilemapLayers.Length == 0 || tilemapLayers[0] == null)
            return;
        UpdateChunksAroundPlayer();
        DespawnDistantChunks();
    }

    void UpdateChunksAroundPlayer()
    {
        Vector3Int playerCell = tilemapLayers[0].WorldToCell(playerTransform.position);
        int playerChunkX = Mathf.FloorToInt(playerCell.x / (float)chunkSize);

        // Spawn chunks within chunkSpawnDistance
        for (int dx = -chunkSpawnDistance; dx <= chunkSpawnDistance; dx++)
        {
            int chunkX = playerChunkX + dx;
            Vector3Int chunkOrigin = new Vector3Int(chunkX * chunkSize, 0, 0);
            if (!loadedChunks.ContainsKey(chunkOrigin))
            {
                loadedChunks[chunkOrigin] = new Chunk(chunkOrigin, chunkSize, tilemapLayers);
            }
        }
    }

    void DespawnDistantChunks()
    {
        Vector3Int playerCell = tilemapLayers[0].WorldToCell(playerTransform.position);
        int playerChunkX = Mathf.FloorToInt(playerCell.x / (float)chunkSize);

        var toRemove = new List<Vector3Int>();
        foreach (var pair in loadedChunks)
        {
            int chunkX = Mathf.FloorToInt(pair.Key.x / (float)chunkSize);
            int distance = Mathf.Abs(chunkX - playerChunkX);
            if (distance > chunkDespawnDistance)
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (var key in toRemove)
        {
            loadedChunks.Remove(key);
            // Optionally: clean up chunk layer tiles, cache, etc.
        }
    }

    private void OnDrawGizmos()
    {
        if (!showChunkGizmos || tilemapLayers == null || tilemapLayers.Length == 0 || tilemapLayers[0] == null)
            return;

        Gizmos.color = chunkGizmoColor;
        foreach (var pair in loadedChunks)
        {
            Vector3Int origin = pair.Key;
            Vector3 worldOrigin = tilemapLayers[0].CellToWorld(origin);
            Vector3 size = new Vector3(chunkSize, chunkSize, 1);
            Gizmos.DrawCube(worldOrigin + size * 0.5f, size);
        }
    }

    /// <summary>
    /// Modular Chunk class holds tilemap layer references and origin.
    /// Extend with biome, archive, etc. if needed.
    /// </summary>
    public class Chunk
    {
        public Vector3Int origin;
        public int size;
        public Tilemap[] layers;

        public Chunk(Vector3Int origin, int size, Tilemap[] layers)
        {
            this.origin = origin;
            this.size = size;
            this.layers = layers;
        }
    }
}