using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldArchiveManager : MonoBehaviour
{
    public bool enableWorldArchive = true;
    public ChunkedWorldArchive worldArchive { get; private set; }
    [SerializeField] public int chunkSize = 16;
    [SerializeField] public BiomeManager biomeManager;

    public void Init(string seedString, bool enableArchive)
    {
        enableWorldArchive = enableArchive;
        if (enableWorldArchive)
            worldArchive = new ChunkedWorldArchive(seedString);
        else
            worldArchive = null;
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
        Func<int, int, int, int> getSurfaceY,      // 3-arg version
        Func<int, int, int, int> getChunkBuffer,   // 3-arg version
        Func<int, string> getBiomeTag,
        int worldBottomY
    )
    {
        if (!(enableWorldArchive && worldArchive != null)) return;
        var existing = worldArchive.TryGetTile(pos);
        if (existing != null) return;

        int surfaceY = getSurfaceY(x, y, z); // <-- pass 3 arguments!
        bool isCave = caveUtility != null && caveUtility.IsCaveAt(x, y, z, surfaceY);

        int buffer = getChunkBuffer(chunkX, z, y); // <-- pass 3 arguments!

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

        worldArchive.SetTile(pos, new TileData { blockTagOrName = tag, discovered = shouldDiscover });
    }

    public TileData TryGetTile(Vector3Int pos)
    {
        if (enableWorldArchive && worldArchive != null)
            return worldArchive.TryGetTile(pos);
        return null;
    }

    public void SetTile(Vector3Int pos, TileData data)
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SetTile(pos, data);
    }

    public void RemoveTile(Vector3Int pos)
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.RemoveTile(pos);
    }

    public void SaveAll()
    {
        if (enableWorldArchive && worldArchive != null)
            worldArchive.SaveAll();
    }

    public void ModifyTile(Vector3Int pos, string tag)
    {
        SetTile(pos, new TileData { blockTagOrName = tag });
    }

    public void DeleteTile(Vector3Int pos, Tilemap groundTilemap, System.Collections.Generic.HashSet<Vector3Int> deletedTiles)
    {
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);

        RemoveTile(pos);

        deletedTiles.Add(pos);
    }

    public void RefreshGroundTile(
        Vector3Int pos,
        Func<int, int, int> getChunkBiome,
        Func<Vector3Int, int, TileBase> resolveTileFromTag,
        Func<int, int, float> getHillValue,
        float hillHeight,
        Tilemap groundTilemap)
    {
        int biomeIndex = getChunkBiome(pos.x / chunkSize, pos.z);
        TileData tileData = TryGetTile(pos);
        TileBase tile = null;
        if (tileData != null)
        {
            tile = resolveTileFromTag(pos, biomeIndex);
        }
        else if (biomeManager != null)
        {
            float hillValue = getHillValue(pos.x, pos.z);
            int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
            var biome = biomeManager.GetBiome(biomeIndex);
            if (biome != null)
            {
                if (pos.y == surfaceY)
                    tile = biome.surfaceTile;
                else if (pos.y < surfaceY)
                    tile = biome.groundTile;
            }
        }

        if (groundTilemap != null)
            groundTilemap.SetTile(pos, tile);
    }

    // --- Add this helper for biome tag retrieval ---
    public string GetBiomeTag(int biomeIdx)
    {
        if (biomeManager != null && biomeManager.biomes != null &&
            biomeIdx >= 0 && biomeIdx < biomeManager.biomes.Count)
            return biomeManager.biomes[biomeIdx].name.Trim().ToLower();
        return "untagged";
    }
}