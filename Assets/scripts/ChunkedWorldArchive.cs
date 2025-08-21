using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChunkedWorldArchive
{
    private const int ChunkSize = 16;
    private const int MaxChunksInMemory = 10;
    private const int SaveEveryNModifications = 20; // Save after every 20 modifications

    // Use LRU ordering for chunk keys
    private Dictionary<(int x, int z), Chunk> loadedChunks = new Dictionary<(int x, int z), Chunk>();
    private LinkedList<(int x, int z)> chunkOrder = new LinkedList<(int x, int z)>();
    private HashSet<(int x, int z)> modifiedChunks = new HashSet<(int x, int z)>();
    private string saveFolder;
    private bool hasManualSave = false;

    // Track number of modifications to trigger auto-save
    private int modificationCounter = 0;

    public ChunkedWorldArchive(string seed)
    {
        // Save path: Application.persistentDataPath/mygame/WorldSaves/{seed}/chunks/
        string baseFolder = Path.Combine(Application.persistentDataPath, "mygame");
        saveFolder = Path.Combine(baseFolder, "WorldSaves", seed, "chunks");
        Directory.CreateDirectory(saveFolder); // Create if not exists
        Application.quitting += OnAppQuit;
    }

    public void SetTile(Vector3Int pos, TileData data)
    {
        var chunkCoords = WorldToChunk(pos);
        var chunk = LoadOrCreateChunk(chunkCoords.x, chunkCoords.z);
        chunk.SetTile(pos, data);
        TouchChunk(chunkCoords);
        modifiedChunks.Add(chunkCoords);
        modificationCounter++;
        EnsureChunkLimit();
        MaybeAutoSave();
    }

    public void RemoveTile(Vector3Int pos)
    {
        var chunkCoords = WorldToChunk(pos);
        if (loadedChunks.TryGetValue(chunkCoords, out var chunk))
        {
            chunk.RemoveTile(pos);
            TouchChunk(chunkCoords);
            modifiedChunks.Add(chunkCoords);
            modificationCounter++;
            EnsureChunkLimit();
            MaybeAutoSave();
        }
    }

    public TileData TryGetTile(Vector3Int pos)
    {
        var chunkCoords = WorldToChunk(pos);
        if (loadedChunks.TryGetValue(chunkCoords, out var chunk))
        {
            TouchChunk(chunkCoords);
            return chunk.TryGetTile(pos);
        }
        // Try load from disk
        if (File.Exists(ChunkPath(chunkCoords.x, chunkCoords.z)))
        {
            var loaded = LoadChunkFromDisk(chunkCoords.x, chunkCoords.z);
            loadedChunks[chunkCoords] = loaded;
            chunkOrder.AddLast(chunkCoords);
            TouchChunk(chunkCoords);
            return loaded.TryGetTile(pos);
        }
        return null;
    }

    public void SaveAll()
    {
        foreach (var chunkCoord in modifiedChunks)
        {
            if (loadedChunks.TryGetValue(chunkCoord, out var chunk))
            {
                SaveChunkToDisk(chunkCoord.x, chunkCoord.z, chunk);
            }
        }
        hasManualSave = true;
        modifiedChunks.Clear();
        modificationCounter = 0; // Reset after save
    }

    private void MaybeAutoSave()
    {
        if (modificationCounter >= SaveEveryNModifications)
        {
            SaveAll();
        }
    }

    public void UnloadDistantChunks(Vector3Int playerPos)
    {
        int playerChunkX = Mathf.FloorToInt((float)playerPos.x / ChunkSize);
        int playerChunkZ = Mathf.FloorToInt((float)playerPos.z / ChunkSize);
        var toRemove = new List<(int x, int z)>();
        foreach (var chunkCoord in loadedChunks.Keys)
        {
            int dx = Math.Abs(chunkCoord.x - playerChunkX);
            int dz = Math.Abs(chunkCoord.z - playerChunkZ);
            if (dx > 5 || dz > 5) // ~50 chunks in 2D ring
            {
                if (modifiedChunks.Contains(chunkCoord))
                {
                    SaveChunkToDisk(chunkCoord.x, chunkCoord.z, loadedChunks[chunkCoord]);
                    modifiedChunks.Remove(chunkCoord);
                }
                toRemove.Add(chunkCoord);
            }
        }
        foreach (var coord in toRemove)
        {
            loadedChunks.Remove(coord);
            chunkOrder.Remove(coord);
        }
    }

    private void EnsureChunkLimit()
    {
        // If too many loaded, remove least recently used
        while (loadedChunks.Count > MaxChunksInMemory)
        {
            var oldest = chunkOrder.First.Value;
            if (modifiedChunks.Contains(oldest))
            {
                SaveChunkToDisk(oldest.x, oldest.z, loadedChunks[oldest]);
                modifiedChunks.Remove(oldest);
            }
            loadedChunks.Remove(oldest);
            chunkOrder.RemoveFirst();
        }
    }

    private void TouchChunk((int x, int z) chunkCoord)
    {
        // Moves chunk to end of LRU list (most recently used)
        chunkOrder.Remove(chunkCoord);
        chunkOrder.AddLast(chunkCoord);
    }

    private (int x, int z) WorldToChunk(Vector3Int pos)
    {
        return (Mathf.FloorToInt((float)pos.x / ChunkSize), Mathf.FloorToInt((float)pos.z / ChunkSize));
    }

    // Serialization helpers
    private string ChunkPath(int x, int z)
        => Path.Combine(saveFolder, $"chunk_{x}_{z}.json");

    private Chunk LoadOrCreateChunk(int x, int z)
    {
        if (loadedChunks.TryGetValue((x, z), out var chunk))
        {
            TouchChunk((x, z));
            return chunk;
        }
        if (File.Exists(ChunkPath(x, z)))
        {
            chunk = LoadChunkFromDisk(x, z);
        }
        else
        {
            chunk = new Chunk(x, z);
        }
        loadedChunks[(x, z)] = chunk;
        chunkOrder.AddLast((x, z));
        return chunk;
    }

    private void SaveChunkToDisk(int x, int z, Chunk chunk)
    {
        string path = ChunkPath(x, z);
        if (chunk.tiles == null || chunk.tiles.Count == 0)
        {
            // Delete chunk file if empty
            if (File.Exists(path))
                File.Delete(path);
        }
        else
        {
            File.WriteAllText(path, JsonUtility.ToJson(chunk));
        }
    }

    private Chunk LoadChunkFromDisk(int x, int z)
    {
        string path = ChunkPath(x, z);
        return JsonUtility.FromJson<Chunk>(File.ReadAllText(path));
    }

    // On quit, delete save folder if no manual save
    private void OnAppQuit()
    {
        if (!hasManualSave)
        {
            var grandParentDir = Directory.GetParent(Directory.GetParent(saveFolder).FullName).FullName;
            if (Directory.Exists(grandParentDir))
                Directory.Delete(grandParentDir, true);
        }
    }
}

// Example chunk class (optimized)
[Serializable]
public class Chunk
{
    public int chunkX, chunkZ;

    // Use a dictionary for tile lookup
    public Dictionary<string, TileData> tiles = new Dictionary<string, TileData>();

    public Chunk(int x, int z) { chunkX = x; chunkZ = z; }

    public void SetTile(Vector3Int pos, TileData data)
    {
        string key = GetKey(pos);
        tiles[key] = data;
    }

    public void RemoveTile(Vector3Int pos)
    {
        string key = GetKey(pos);
        tiles.Remove(key);
    }

    public TileData TryGetTile(Vector3Int pos)
    {
        string key = GetKey(pos);
        return tiles.TryGetValue(key, out var data) ? data : null;
    }

    private static string GetKey(Vector3Int pos) => $"{pos.x},{pos.y},{pos.z}";
}

[Serializable]
public class TileDataEntry
{
    public int x, y, z;
    public TileData data;
    public TileDataEntry(Vector3Int pos, TileData data) { x = pos.x; y = pos.y; z = pos.z; this.data = data; }
}