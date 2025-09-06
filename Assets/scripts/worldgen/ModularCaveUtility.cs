using UnityEngine;
using UnityEngine.Tilemaps;

public class ModularCaveUtility : MonoBehaviour
{
    public InfiniteCameraSpawnerModular spawner;

    // --- Toggle modular cave types ---
    [Header("Cave Mode Toggles")]
    public bool surfaceCavesEnabled = true;
    public bool subsurfaceCavesEnabled = true;
    public bool deepCavesEnabled = true;

    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.18f;
    public float caveThreshold = 0.62f;
    public float caveHorizontalMultiplier = 1.0f;
    public float caveVerticalMultiplier = 1.0f;
    [Range(0.5f, 5f)] public float caveSharpness = 1.7f;
    public float secondaryFrequency = 0.16f;
    [Range(0f, 1f)] public float secondaryWeight = 0.28f;
    [Range(-1f, 1f)] public float verticalBias = 0.0f;
    [Range(0f, 1f)] public float verticalBiasStrength = 0.0f;

    [Header("Cave Shape/Curve Controls")]
    public float caveCurve = 0.0f;
    public float caveVerticalScale = 0.0f;
    public float caveHorizontalScale = 0.0f;

    [Header("Deep Cave Controls")]
    public int surfaceDepth = 100;
    public int tunnelTargetDepth = 44;
    public int tunnelBranchLength = 60;

    [Header("Big Room Controls")]
    public float roomFrequency = 0.045f;
    public float roomThreshold = 0.72f;
    public float roomSizeMultiplier = 2.3f;

    [Header("Subsurface Cave Controls")]
    public float subsurfaceCaveFrequency = 0.22f;
    public float subsurfaceCaveThreshold = 0.69f;
    public float subsurfaceCaveSizeMultiplier = 0.18f;
    public int subsurfaceMinDepth = 7;   // How far below surface these start
    public int subsurfaceMaxDepth = 22;  // How far below surface these stop

    [Header("Cave Entrance Controls")]
    [Range(0f, 1f)] public float caveEntranceChance = 0.26f;
    [Range(0f, 2f)] public float caveEntranceZigzag = 0.8f;
    [Range(2, 8)] public int caveEntranceWidth = 2;
    public int caveEntranceAbove = 10;
    public int caveEntranceBelow = 20;
    public int caveExitYOffset = 50;
    public int caveExitXOffset = 60;

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;
    public TileBase visibleCaveTileAsset2;
    [Tooltip("Main cave tilemap.")] public Tilemap caveTilemap;
    [Tooltip("Fog tilemap.")] public Tilemap caveTilemapFog;
    [Header("Hidden/Backup Tilemap")]
    [Tooltip("Hidden cave tilemap.")] public Tilemap caveTilemapHidden;

    public Transform playerTransform;
    public bool IsInitialized { get; private set; } = false;

    public int minX = int.MinValue;
    public int maxX = int.MaxValue;
    public int minY = int.MinValue;
    public int maxY = int.MaxValue;
    public int z = 0;

    [Header("Debug Controls (Normal)")]
    public bool debug = true;
    public int targetZLayer = 0;

    [Header("Debug Controls (Fog)")]
    public bool debugFog = true;
    public int targetZLayerFog = 0;

    [Header("Delete Caves In Radius")]
    public bool deleteCavesInRadius = false;
    public float deleteCavesRadius = 10f;
    public ChunkedWorldArchive archive;

    [Header("Persistent Delete Settings")]
    public int persistentDeleteCount = 20;
    public float persistentDeleteDuration = 10f;


    private Coroutine persistentDeleteCoroutine;

    void Awake() { IsInitialized = true; }

    /// <summary>
    /// Returns true if this position should be a cave (surface tunnels, subsurface, or deep rooms).
    /// </summary>
    public bool IsCave(int x, int y, int z, int surfaceY)
    {
        Vector3Int pos = new Vector3Int(x, y, z);

   

        // Never spawn if already discovered
        if (archive != null && archive.AllTiles().TryGetValue(pos, out TileData tileData))
        {
            if (tileData != null && tileData.blockTagOrName == "cave" && tileData.discovered)
                return false;
        }

        // --- SURFACE TUNNELS ---
        if (surfaceCavesEnabled && IsSurfaceTunnelCave(x, y, z, surfaceY))
            return true;

        // --- SUBSURFACE CAVES ---
        if (subsurfaceCavesEnabled && IsSubsurfaceCave(x, y, z, surfaceY))
            return true;

        // --- DEEP ROOMS/TUNNELS ---
        if (deepCavesEnabled && IsDeepRoomCave(x, y, z, surfaceY))
            return true;

        return false;
    }

    /// <summary>
    /// Surface tunnels: caves branch down from entrances, only slightly under the surface, then tunnel down.
    /// </summary>
    public bool IsSurfaceTunnelCave(int x, int y, int z, int surfaceY)
    {
        int entranceDepth = 7;
        int minTunnelY = surfaceY - entranceDepth;
        int maxTunnelY = surfaceY;
        if (y > maxTunnelY || y < minTunnelY - tunnelTargetDepth - tunnelBranchLength) return false;

        if (y >= surfaceY - 2 && y <= surfaceY)
        {
            for (int dx = -caveEntranceWidth / 2; dx <= caveEntranceWidth / 2; dx++)
            {
                if (IsCaveEntranceSingle(x + dx, y, z, surfaceY) &&
                    (IsCaveEntranceSingle(x + dx, y + 1, z, surfaceY) ||
                     IsCaveEntranceSingle(x + dx, y - 1, z, surfaceY)))
                {
                    return true;
                }
            }
            return false;
        }

        int targetTunnelY = surfaceY - tunnelTargetDepth;
        float tunnelNoise1 = Mathf.PerlinNoise(
            (x + z * 13) * caveFrequency * 2.5f,
            (y - targetTunnelY) * 0.16f + z * 0.07f
        );
        float tunnelNoise2 = Mathf.PerlinNoise(
            (x - z * 8) * secondaryFrequency * 2.0f,
            (y + z * 5) * secondaryFrequency * 0.13f
        );
        float tunnelDepthBias = Mathf.Exp(-Mathf.Abs(y - targetTunnelY) / 14.0f);

        bool isTunnel = ((tunnelNoise1 * tunnelDepthBias > 0.52f) || (tunnelNoise2 * tunnelDepthBias > 0.54f));

        return isTunnel;
    }

    /// <summary>
    /// Subsurface caves: smaller, blob-like caves just below surface, not tunnels.
    /// </summary>
    public bool IsSubsurfaceCave(int x, int y, int z, int surfaceY)
    {
        int minY = surfaceY - subsurfaceMinDepth;
        int maxY = surfaceY - subsurfaceMaxDepth;
        if (y <= maxY || y >= minY) return false;

        float blobFx = x * subsurfaceCaveFrequency;
        float blobFy = y * subsurfaceCaveFrequency;
        float blobFz = z * subsurfaceCaveFrequency;

        float blobNoise = Mathf.PerlinNoise(blobFx + blobFz, blobFy + blobFz);
        bool isBlob = blobNoise * subsurfaceCaveSizeMultiplier > subsurfaceCaveThreshold;

        return isBlob;
    }

    /// <summary>
    /// Deep caves: smaller rooms/chambers and more, smaller tunnels far below surface.
    /// </summary>
    public bool IsDeepRoomCave(int x, int y, int z, int surfaceY)
    {
        int minDeepCaveY = surfaceY - 35;
        if (y >= minDeepCaveY) return false;

        float roomFx = x * roomFrequency;
        float roomFy = y * roomFrequency;
        float roomFz = z * roomFrequency;
        float roomNoise = Mathf.PerlinNoise(roomFx + roomFz, roomFy + roomFz);

        bool isRoom = roomNoise * (roomSizeMultiplier * 0.35f) > (roomThreshold + 0.22f);

        float tunnelNoiseA = Mathf.PerlinNoise(
            (x + z * 17) * caveFrequency * 2.0f,
            (y + z * 7) * caveFrequency * 1.7f
        );
        float tunnelNoiseB = Mathf.PerlinNoise(
            (x - z * 19) * secondaryFrequency * 2.7f,
            (y + z * 4) * secondaryFrequency * 1.2f
        );
        bool isTunnel = (tunnelNoiseA > caveThreshold + 0.05f) || (tunnelNoiseB > caveThreshold + 0.07f);

        return isRoom || isTunnel;
    }

    public bool IsCaveEntranceSingle(int x, int y, int z, int surfaceY)
    {
        int entranceTop = surfaceY + caveEntranceAbove;
        int entranceBottom = surfaceY - caveEntranceBelow;
        if (y > entranceTop || y < entranceBottom - caveExitYOffset) return false;

        int entranceSeed = x * 73856093 ^ z * 19349663;
        System.Random entranceRand = new System.Random(entranceSeed);

        if (entranceRand.NextDouble() >= caveEntranceChance)
            return false;

        bool exitLeft = entranceRand.NextDouble() < 0.5;
        int exitY = entranceBottom - caveExitYOffset;
        int exitX = x + (exitLeft ? -caveExitXOffset : caveExitXOffset);

        float t = Mathf.InverseLerp(entranceTop, exitY, y);
        float curvedT = Mathf.Pow(t, 1.7f);
        float idealX = Mathf.Lerp(x, exitX, curvedT);

        float zigzag = Mathf.Sin((y - entranceTop) * 0.7f + entranceSeed) *
                       caveEntranceZigzag *
                       (caveEntranceBelow + caveEntranceAbove + caveExitYOffset) *
                       0.4f *
                       Mathf.Clamp01(t + 0.1f);

        float centerX = idealX + zigzag;

        return Mathf.Abs(x - centerX) <= (caveEntranceWidth / 2f);
    }
}