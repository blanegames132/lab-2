using System;

public static class ChunkManagerBus
{
    public static event Action<ChunkSpawnRequest> OnChunkSpawnRequest;
    public static event Action<ChunkDespawnRequest> OnChunkDespawnRequest;

    public static void RequestChunkSpawn(int chunkX, int chunkY, int chunkSize, int[] zOffsets)
    {
        OnChunkSpawnRequest?.Invoke(new ChunkSpawnRequest(chunkX, chunkY, chunkSize, zOffsets));
    }

    public static void RequestChunkDespawn(int chunkX, int chunkY, int chunkSize, int[] zOffsets)
    {
        OnChunkDespawnRequest?.Invoke(new ChunkDespawnRequest(chunkX, chunkY, chunkSize, zOffsets));
    }
}

public class ChunkSpawnRequest
{
    public int chunkX, chunkY, chunkSize;
    public int[] zOffsets;
    public ChunkSpawnRequest(int x, int y, int size, int[] z)
    {
        chunkX = x; chunkY = y; chunkSize = size; zOffsets = z;
    }
}

public class ChunkDespawnRequest
{
    public int chunkX, chunkY, chunkSize;
    public int[] zOffsets;
    public ChunkDespawnRequest(int x, int y, int size, int[] z)
    {
        chunkX = x; chunkY = y; chunkSize = size; zOffsets = z;
    }
}