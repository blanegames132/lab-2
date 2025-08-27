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

    /// <summary>
    /// Returns the tag of the tile (e.g. "air", "cave", or biome tag string) for fog logic.
    /// </summary>
    public string GetTileTagForFog(Vector3Int pos, string fallback)
    {
        if (!isInitialized || worldArchive == null) return fallback;

        TileData tileData = worldArchive.TryGetTile(pos);
        if (tileData != null)
            return tileData.blockTagOrName;
        // If not found, fallback logic
        return fallback;
    }

    /// <summary>
    /// Modify a tile by its string tag (e.g. "air", "cave", or biome name from inspector).
    /// </summary>
    public void ModifyTile(Vector3Int pos, string tag)
    {
        if (!isInitialized || worldArchive == null) return;
        worldArchive.SetTile(pos, new TileData { blockTagOrName = tag });
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