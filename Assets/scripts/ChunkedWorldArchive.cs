using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//
// ===============================
// Chunked World Archive
// ===============================
// Handles saving/loading chunks to .json so tiles persist.
//
public class ChunkedWorldArchive
{
    private const int ChunkSize = 16;
    private const int MaxChunksInMemory = 10;
    private const int SaveEveryNModifications = 20;
    private const int UnloadBeyondChunks = 12; // safety bound for manual unload by distance

    private readonly Dictionary<(int x, int z), Chunk> loadedChunks = new Dictionary<(int x, int z), Chunk>();
    private readonly LinkedList<(int x, int z)> chunkOrder = new LinkedList<(int x, int z)>();
    private readonly HashSet<(int x, int z)> modifiedChunks = new HashSet<(int x, int z)>();

    private readonly string saveFolder;
    private bool hasManualSave = false;
    private int modificationCounter = 0;

    public ChunkedWorldArchive(string seed)
    {
        string baseFolder = Path.Combine(Application.persistentDataPath, "mygame");
        saveFolder = Path.Combine(baseFolder, "WorldSaves", seed, "chunks");
        Directory.CreateDirectory(saveFolder);
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

    public void RemoveTile(Vector3Int pos)
    {
        var chunkCoords = WorldToChunk(pos);
        var chunk = LoadOrCreateChunk(chunkCoords.x, chunkCoords.z);
        if (chunk.RemoveTile(pos))
        {
            modifiedChunks.Add(chunkCoords);
            modificationCounter++;
            MaybeAutoSave();
        }
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
        modificationCounter = 0;
    }

    public void UnloadDistantChunks(Vector3Int playerWorldPos)
    {
        int playerChunkX = Mathf.FloorToInt((float)playerWorldPos.x / ChunkSize);

        var toUnload = new List<(int x, int z)>();
        foreach (var kvp in loadedChunks)
        {
            var (cx, cz) = kvp.Key;
            if (Mathf.Abs(cx - playerChunkX) > UnloadBeyondChunks)
            {
                toUnload.Add(kvp.Key);
            }
        }

        foreach (var coord in toUnload)
        {
            if (modifiedChunks.Contains(coord))
            {
                SaveChunkToDisk(coord.x, coord.z, loadedChunks[coord]);
                modifiedChunks.Remove(coord);
            }
            loadedChunks.Remove(coord);
            chunkOrder.Remove(coord);
        }
    }

    private void MaybeAutoSave()
    {
        if (modificationCounter >= SaveEveryNModifications)
        {
            SaveAll();
        }
    }

    private void EnsureChunkLimit()
    {
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
        chunkOrder.Remove(chunkCoord);
        chunkOrder.AddLast(chunkCoord);
    }

    private (int x, int z) WorldToChunk(Vector3Int pos)
    {
        return (Mathf.FloorToInt((float)pos.x / ChunkSize),
                Mathf.FloorToInt((float)pos.z / ChunkSize));
    }

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
        if (chunk.IsEmpty)
        {
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

    private void OnAppQuit()
    {
        if (!hasManualSave)
        {
            // Delete entire seed folder if user never saved manually
            var seedFolder = Directory.GetParent(saveFolder)?.FullName;
            if (!string.IsNullOrEmpty(seedFolder) && Directory.Exists(seedFolder))
            {
                try
                {
                    Directory.Delete(seedFolder, true);
                    Debug.Log($"[Archive] Deleted unsaved world at {seedFolder}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Archive] Failed to delete unsaved world: {e}");
                }
            }
        }
        else
        {
            SaveAll();
        }
    }
}

[Serializable]
public class Chunk : ISerializationCallbackReceiver
{
    public int chunkX, chunkZ;

    // Unity's JsonUtility can't serialize Dictionary directly.
    // We'll store parallel lists for serialization and rebuild the dictionary at runtime.
    [NonSerialized] private Dictionary<string, TileData> tiles = new Dictionary<string, TileData>();
    public List<string> keys = new List<string>();
    public List<TileData> values = new List<TileData>();

    public bool IsEmpty => tiles == null || tiles.Count == 0;

    public Chunk(int x, int z) { chunkX = x; chunkZ = z; }

    public void SetTile(Vector3Int pos, TileData data)
    {
        if (tiles == null) tiles = new Dictionary<string, TileData>();
        string key = GetKey(pos);
        tiles[key] = data;
    }

    public TileData TryGetTile(Vector3Int pos)
    {
        if (tiles == null) tiles = new Dictionary<string, TileData>();
        tiles.TryGetValue(GetKey(pos), out var data);
        return data;
    }

    public bool RemoveTile(Vector3Int pos)
    {
        if (tiles == null || tiles.Count == 0) return false;
        return tiles.Remove(GetKey(pos));
    }

    private static string GetKey(Vector3Int pos) => $"{pos.x},{pos.y},{pos.z}";

    // --- Serialization glue ---
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        if (tiles == null) return;
        foreach (var kv in tiles)
        {
            keys.Add(kv.Key);
            values.Add(kv.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        tiles = new Dictionary<string, TileData>(keys.Count);
        for (int i = 0; i < keys.Count; i++)
        {
            // values[i] can be null if file was corrupted; guard
            if (!tiles.ContainsKey(keys[i]) && values[i] != null)
                tiles.Add(keys[i], values[i]);
        }
    }
}