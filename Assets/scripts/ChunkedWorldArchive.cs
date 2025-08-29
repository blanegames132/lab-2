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
    private const int UnloadBeyondChunks = 12;

    private readonly Dictionary<(int x, int z), Chunk> loadedChunks = new();
    private readonly LinkedList<(int x, int z)> chunkOrder = new();
    private readonly HashSet<(int x, int z)> modifiedChunks = new();

    private readonly string saveFolder;
    private bool hasManualSave = false;
    private int modificationCounter = 0;

    private static readonly object FileIoLock = new object();
    private Dictionary<Vector2Int, int> chunkBiomes = new();

    /// <summary>
    /// Returns all world tile positions and data (from loaded/in-memory chunks only, for speed)
    /// </summary>
    public Dictionary<Vector3Int, TileData> AllTiles()
    {
        var allTiles = new Dictionary<Vector3Int, TileData>();
        foreach (var chunkPair in loadedChunks)
        {
            foreach (var tilePair in chunkPair.Value.GetAllTiles())
            {
                allTiles[tilePair.Key] = tilePair.Value;
            }
        }
        return allTiles;
    }

    public bool HasChunk(Vector2Int key)
    {
        return chunkBiomes.ContainsKey(key);
    }

    public ChunkedWorldArchive(string seed)
    {
        string baseFolder = Path.Combine(Application.persistentDataPath, "mygame");
        string worldFolder = Path.Combine(baseFolder, "WorldSaves", seed);
        saveFolder = Path.Combine(worldFolder, "chunks");

        if (!Directory.Exists(saveFolder))
            Directory.CreateDirectory(saveFolder);

        Application.quitting += OnAppQuit;
    }

    // -----------------------------
    // Tile Operations
    // -----------------------------
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

    // -----------------------------
    // Save/Unload
    // -----------------------------
    public void SaveAll()
    {
        var toSave = new List<(int x, int z)>(modifiedChunks);

        foreach (var chunkCoord in toSave)
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

    // -----------------------------
    // Helpers
    // -----------------------------
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

        lock (FileIoLock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string json = JsonUtility.ToJson(chunk);
            string tmpPath = path + ".tmp";

            using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(true);
            }

            try
            {
                if (File.Exists(path))
                {
#if NETSTANDARD || NET_4_6 || NET_4_6_1 || NET_4_7 || NET_4_7_1
                    File.Replace(tmpPath, path, null);
#else
                    File.Delete(path);
                    File.Move(tmpPath, path);
#endif
                }
                else
                {
                    File.Move(tmpPath, path);
                }
            }
            catch (IOException)
            {
                try
                {
                    File.Copy(tmpPath, path, true);
                }
                finally
                {
                    if (File.Exists(tmpPath)) File.Delete(tmpPath);
                }
            }
            catch
            {
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
                throw;
            }
        }
    }

    private Chunk LoadChunkFromDisk(int x, int z)
    {
        string path = ChunkPath(x, z);
        lock (FileIoLock)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs))
            {
                var json = sr.ReadToEnd();
                var chunk = JsonUtility.FromJson<Chunk>(json);
                if (chunk == null) chunk = new Chunk(x, z);
                return chunk;
            }
        }
    }

    private void OnAppQuit()
    {
        SaveAll();
    }
}

[Serializable]
public class Chunk : ISerializationCallbackReceiver
{
    public int chunkX, chunkZ;

    [NonSerialized] private Dictionary<string, TileData> tiles = new();
    public List<string> keys = new();
    public List<TileData> values = new();

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

    public Dictionary<Vector3Int, TileData> GetAllTiles()
    {
        var dict = new Dictionary<Vector3Int, TileData>();
        if (tiles == null) return dict;
        foreach (var kv in tiles)
        {
            if (TryParseKey(kv.Key, out var pos))
                dict[pos] = kv.Value;
        }
        return dict;
    }

    private static string GetKey(Vector3Int pos) => $"{pos.x},{pos.y},{pos.z}";

    private static bool TryParseKey(string key, out Vector3Int pos)
    {
        var parts = key.Split(',');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out int x) &&
            int.TryParse(parts[1], out int y) &&
            int.TryParse(parts[2], out int z))
        {
            pos = new Vector3Int(x, y, z);
            return true;
        }
        pos = default;
        return false;
    }

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
            if (!tiles.ContainsKey(keys[i]) && values[i] != null)
                tiles.Add(keys[i], values[i]);
        }
    }
}