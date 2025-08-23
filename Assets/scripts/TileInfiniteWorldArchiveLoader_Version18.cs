using System;
using UnityEngine;

public class TileInfiniteWorldArchiveLoader : MonoBehaviour
{
    [Header("Hill Randomization")]
    [SerializeField] public string hillRandomSeed = "";
    [SerializeField] private int generatedSeedHash;
    [SerializeField] private string usedSeedString;

    [Header("Advanced Seed Controls")]
    [SerializeField] private float repeatRange;
    [SerializeField] private float curveShift;
    [SerializeField] private float perlinOffsetX;
    [SerializeField] private float perlinOffsetZ;
    [SerializeField] private float perlinStrength;
    [SerializeField] private float perlinBase;

    [Header("World Controls")]
    [SerializeField] private int worldBottomY;

    private ChunkedWorldArchive worldArchive;
    private bool isInitialized = false;

    public void InitArchive()
    {
        if (isInitialized) return;

        if (string.IsNullOrEmpty(hillRandomSeed) || hillRandomSeed.ToLower() == "random")
            hillRandomSeed = DateTime.Now.Ticks.ToString();

        usedSeedString = hillRandomSeed;
        worldArchive = new ChunkedWorldArchive(usedSeedString);
        isInitialized = true;
    }

    public TileType GetTileTypeForFog(Vector3Int pos, TileType fallback)
    {
        if (!isInitialized || worldArchive == null) return fallback;

        TileData tileData = worldArchive.TryGetTile(pos);
        if (tileData != null)
            return tileData.type;
        // If not found, fallback logic
        return fallback;
    }

    public void ModifyTile(Vector3Int pos, TileType type)
    {
        if (!isInitialized || worldArchive == null) return;
        worldArchive.SetTile(pos, new TileData { type = type });
    }

    public void DeleteTile(Vector3Int pos)
    {
        if (!isInitialized || worldArchive == null) return;
        worldArchive.RemoveTile(pos);
    }

    public void SaveGame()
    {
        if (!isInitialized || worldArchive == null) return;
        worldArchive.SaveAll();
    }

    public void UnloadDistantChunks(Vector3Int playerPos)
    {
        if (!isInitialized || worldArchive == null) return;
        worldArchive.UnloadDistantChunks(playerPos);
    }
}