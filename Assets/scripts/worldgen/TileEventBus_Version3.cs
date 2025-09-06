using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileEventBus
{
    public static event System.Action<int, int, int> OnChunkSpawnRequest;
    public static event System.Action<int, int, int> OnChunkDespawnRequest;
    public static event System.Action<Tilemap, Vector3Int> OnTileDelete;
    public static event System.Action<Tilemap, Vector3Int, TileBase> OnTileSet;
    public static event System.Func<Vector3Int, bool> ShouldBlockTile;

    // Add this event for resetting all caves to undiscovered
    public static event System.Action OnResetAllCavesToUndiscovered;

    public static void BroadcastChunkSpawnRequest(int chunkX, int chunkY, int chunkSize)
    {
        OnChunkSpawnRequest?.Invoke(chunkX, chunkY, chunkSize);
    }
    public static void BroadcastChunkDespawnRequest(int chunkX, int chunkY, int chunkSize)
    {
        OnChunkDespawnRequest?.Invoke(chunkX, chunkY, chunkSize);
    }
    public static void BroadcastTileDelete(Tilemap tilemap, Vector3Int pos)
    {
        OnTileDelete?.Invoke(tilemap, pos);
    }
    public static void BroadcastTileSet(Tilemap tilemap, Vector3Int pos, TileBase tile)
    {
        OnTileSet?.Invoke(tilemap, pos, tile);
    }
    public static bool QueryShouldBlockTile(Vector3Int pos)
    {
        return ShouldBlockTile?.Invoke(pos) ?? false;
    }

    // Implement this method for broadcasting the cave reset event
    public static void BroadcastResetAllCavesToUndiscovered()
    {
        OnResetAllCavesToUndiscovered?.Invoke();
    }
}