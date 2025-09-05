using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Modular infinite world spawner with seed support via SeedSelector.
/// Attach SeedSelector and call ApplySeedFromSelector at startup to modularly set terrain params.
/// </summary>
public class InfiniteCameraSpawnerModular : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap frontTilemap;
    public Tilemap middleFrontTilemap;
    public Tilemap middleBackTilemap;
    public Tilemap backTilemap;

    [Header("Tiles")]
    public TileBase defaultSurfaceTile;
    public TileBase defaultSubsurfaceTile;
    public TileBase defaultGroundTile;
    public TileBase defaultBedrockTile;

    [Header("Player")]
    public Transform playerTransform;

    [Header("Biome Manager Fallback (optional)")]
    public BiomeManagerFallback biomeManagerFallback;

    [Header("Biome Selection")]
    [Tooltip("Enter biome name (case-insensitive) or index. If both invalid, uses default tiles.")]
    public string selectedBiomeName = "";
    [Tooltip("Biome index. If name is empty or not found, uses index. If both are invalid, uses default tiles.")]
    public int selectedBiomeIndex = 0;

    [Header("Area Size & Depth")]
    public float visualAreaSize = 30f;
    public int tilesBelow = 50;

    [Header("Terrain Generation (set by seed)")]
    public float hillHeight;
    public float seedScale;
    public float hillNoiseScale;
    public float cliffSharpness;
    public float hillVerticalShift;
    public float canyonThreshold;

    [Header("Archiving")]
    public WorldArchiveManager worldArchiveMa; // Assign in Inspector

    [Header("Seed Modular")]
    public SeedSelector seedSelector; // drag your SeedSelector here in Inspector

    // Call this from Awake/Start to modularly set terrain params from your seed source
    public void ApplySeedFromSelector()
    {
        if (seedSelector == null)
        {
            Debug.LogWarning("SeedSelector not assigned!");
            return;
        }

        // Modular seed logic: use only the minimal parameters for demo
        int hash = seedSelector.usedSeedInt;
        System.Random rand = new System.Random(hash);

        seedScale = SeededValue(rand, 0.03f, 0.09f, 1);
        hillHeight = SeededValue(rand, 5f, 24f, 2);
        hillNoiseScale = SeededValue(rand, 0.03f, 0.12f, 3);
        cliffSharpness = SeededValue(rand, 1.2f, 3.0f, 4);
        hillVerticalShift = SeededValue(rand, -8f, 4f, 5);
        canyonThreshold = SeededValue(rand, 0.8f, 0.97f, 6);
    }

    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }

    void Awake()
    {
        // Apply seed before world generation
        ApplySeedFromSelector();
    }

    void OnApplicationQuit()
    {
        if (worldArchiveMa != null)
        {
            worldArchiveMa.SaveAll();
            Debug.Log("WorldArchiveManager: saved on exit.");
        }
    }

    Biome GetActiveBiome()
    {
        if (biomeManagerFallback == null || biomeManagerFallback.biomes == null || biomeManagerFallback.biomes.Count == 0)
            return null;
        return biomeManagerFallback.GetBiome(selectedBiomeName, selectedBiomeIndex);
    }

    TileBase GetTileForType(Biome biome, string type)
    {
        if (biome != null)
        {
            if (type == "surface") return biome.surfaceTile ?? defaultSurfaceTile;
            if (type == "subsurface") return biome.subsurfaceTile ?? defaultSubsurfaceTile;
            if (type == "ground") return biome.groundTile ?? defaultGroundTile;
            if (type == "bedrock") return biome.bedrockTile ?? defaultBedrockTile;
        }
        if (type == "surface") return defaultSurfaceTile;
        if (type == "subsurface") return defaultSubsurfaceTile;
        if (type == "ground") return defaultGroundTile;
        if (type == "bedrock") return defaultBedrockTile;
        return defaultGroundTile;
    }

    public void UpdateWorldIfNeeded()
    {
        if (groundTilemap == null || playerTransform == null)
            return;

        Grid grid = groundTilemap.layoutGrid;
        float cellSizeX = Mathf.Abs(grid.cellSize.x);
        int numCellsX = Mathf.RoundToInt(visualAreaSize / cellSizeX);
        Vector3Int playerCell = groundTilemap.WorldToCell(playerTransform.position);
        int halfCellsX = numCellsX / 2;
        int minX = playerCell.x - halfCellsX;
        int maxX = minX + numCellsX - 1;
        int minY = playerCell.y - tilesBelow + 1;
        int maxY = playerCell.y;
        Biome biome = GetActiveBiome();

        int z = playerCell.z;

        for (int x = minX; x <= maxX; x++)
        {
            float hill = Mathf.PerlinNoise(x * hillNoiseScale, z * hillNoiseScale);
            hill = Mathf.Pow(hill, cliffSharpness);
            float canyonMask = Mathf.PerlinNoise(z * 0.12f, x * 0.015f);

            float height = hill * hillHeight + hillVerticalShift;
            if (canyonMask > canyonThreshold)
                height += hillHeight * 2f;
            else if (canyonMask < 1f - canyonThreshold)
                height -= hillHeight * 2f;

            int surfaceY = Mathf.RoundToInt(height);

            // Surface tile
            Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
            SetTileAll(surfacePos, GetTileForType(biome, "surface"), z);
            ArchiveTile(surfacePos, GetTileForType(biome, "surface"));

            // Archive "air" directly above surface (for world save completeness)
            Vector3Int airPos = new Vector3Int(x, surfaceY + 1, z);
            ArchiveTile(airPos, null);

            // Fill below surface
            for (int y = surfaceY - 1; y >= minY; y--)
            {
                string type = (y == minY) ? "bedrock" : (y > minY + 2) ? "ground" : "subsurface";
                Vector3Int belowPos = new Vector3Int(x, y, z);
                SetTileAll(belowPos, GetTileForType(biome, type), z);
                ArchiveTile(belowPos, GetTileForType(biome, type));
            }
        }
    }

    void ArchiveTile(Vector3Int pos, TileBase tile)
    {
        if (worldArchiveMa != null)
        {
            string tag = tile != null ? tile.name : "air";
            worldArchiveMa.ArchiveTile(pos, tag); // <-- IMMEDIATE SAVE!
        }
    }

    void SetTileAll(Vector3Int pos, TileBase tile, int playerZ)
    {
        if (groundTilemap != null) groundTilemap.SetTile(new Vector3Int(pos.x, pos.y, playerZ), tile);
        if (frontTilemap != null) frontTilemap.SetTile(new Vector3Int(pos.x, pos.y, playerZ + 1), tile);
        if (middleFrontTilemap != null) middleFrontTilemap.SetTile(new Vector3Int(pos.x, pos.y, playerZ + 2), tile);
        if (middleBackTilemap != null) middleBackTilemap.SetTile(new Vector3Int(pos.x, pos.y, playerZ - 1), tile);
        if (backTilemap != null) backTilemap.SetTile(new Vector3Int(pos.x, pos.y, playerZ - 2), tile);
    }

    public void DeleteTile(Vector3Int pos)
    {
        if (groundTilemap != null)
            groundTilemap.SetTile(pos, null);

        if (worldArchiveMa != null)
        {
            worldArchiveMa.DeleteTile(pos, groundTilemap); // <-- IMMEDIATE SAVE!
        }
    }
}