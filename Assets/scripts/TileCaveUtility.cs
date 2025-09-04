using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Keeps track of deleted fog tiles so they never respawn.
/// Persists to JSON in Application.persistentDataPath.
/// </summary>
[System.Serializable]
public class DeletedFogArchive
{
    [System.Serializable]
    private class SerializableVec3Int
    {
        public int x, y, z;
        public SerializableVec3Int(Vector3Int v) { x = v.x; y = v.y; z = v.z; }
        public Vector3Int ToVector3Int() => new Vector3Int(x, y, z);
    }

    [System.Serializable]
    private class SaveData
    {
        public List<SerializableVec3Int> deletedPositions = new List<SerializableVec3Int>();
    }

    private HashSet<Vector3Int> deletedPositions = new HashSet<Vector3Int>();

    /// <summary> Returns true if this gridPos was permanently deleted. </summary>
    public bool IsDeleted(Vector3Int pos) => deletedPositions.Contains(pos);

    /// <summary> Marks this gridPos as permanently deleted. </summary>
    public void MarkDeleted(Vector3Int pos)
    {
        if (!deletedPositions.Contains(pos))
            deletedPositions.Add(pos);
    }

    /// <summary> Saves deleted positions to disk as JSON. </summary>
    public void SaveToFile(string path)
    {
        try
        {
            SaveData data = new SaveData();
            foreach (var pos in deletedPositions)
                data.deletedPositions.Add(new SerializableVec3Int(pos));

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DeletedFogArchive] Failed to save: {ex}");
        }
    }

    /// <summary> Loads deleted positions from disk. </summary>
    public void LoadFromFile(string path)
    {
        if (!File.Exists(path)) return;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            deletedPositions.Clear();
            foreach (var sv in data.deletedPositions)
                deletedPositions.Add(sv.ToVector3Int());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[DeletedFogArchive] Failed to load: {ex}");
        }
    }
}
public class TileCaveUtility : MonoBehaviour
{
    public TileInfiniteCameraSpawner spawner;

    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.09f;
    public float caveThreshold = 0.5f;
    public float caveHorizontalMultiplier = 1.0f;
    public float caveVerticalMultiplier = 1.0f;
    [Range(0.5f, 5f)] public float caveSharpness = 1.0f;
    public float secondaryFrequency = 0.0f;
    [Range(0f, 1f)] public float secondaryWeight = 0.0f;
    [Range(-1f, 1f)] public float verticalBias = 0.0f;
    [Range(0f, 1f)] public float verticalBiasStrength = 0.0f;

    [Header("Cave Shape/Curve Controls")]
    public float caveCurve = 0.0f;
    public float caveVerticalScale = 0.0f;
    public float caveHorizontalScale = 0.0f;

    [Header("Deep Cave Controls")]
    public int surfaceDepth = 100;

    [Header("Cave Entrance Controls")]
    [Range(0f, 1f)] public float caveEntranceChance = 0.3f;
    [Range(0f, 2f)] public float caveEntranceZigzag = 1.0f;
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
    public DeletedFogArchive fogDeleteArchive = new DeletedFogArchive();

    private Coroutine persistentDeleteCoroutine;

    void Awake() { IsInitialized = true; }

    // =============================
    //   CAVE GENERATION FUNCTIONS
    // =============================

    public bool IsCaveAt(int x, int y, int z, int surfaceY)
    {
        Vector3Int pos = new Vector3Int(x, y, z);

        // Never spawn if deleted
        if (fogDeleteArchive != null && fogDeleteArchive.IsDeleted(pos))
            return false;

        // Never spawn if already discovered
        if (archive != null && archive.AllTiles().TryGetValue(pos, out TileData tileData))
        {
            if (tileData != null && tileData.blockTagOrName == "cave" && tileData.discovered)
                return false;
        }

        // Entrance check
        for (int dx = -caveEntranceWidth / 2; dx <= caveEntranceWidth / 2; dx++)
        {
            if (IsCaveEntranceSingle(x + dx, y, z, surfaceY) &&
                (IsCaveEntranceSingle(x + dx, y + 1, z, surfaceY) ||
                 IsCaveEntranceSingle(x + dx, y - 1, z, surfaceY)))
                return true;
        }

        return CaveGenerator(x, y, z, surfaceY) > caveThreshold;
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
                       0.45f *
                       Mathf.Clamp01(t + 0.1f);

        float centerX = idealX + zigzag;

        return Mathf.Abs(x - centerX) <= (caveEntranceWidth / 2f);
    }

    public float CaveGenerator(int x, int y, int z, int surfaceY)
    {
        int caveStartY = surfaceY - surfaceDepth;
        if (y > caveStartY) return 0f;

        float xCurve = x, yCurve = y;
        if (caveCurve != 0f)
        {
            xCurve += Mathf.Sin(y * 0.1f) * caveCurve * 10f;
            yCurve += Mathf.Sin(x * 0.1f + 100f) * caveCurve * 10f;
        }

        float fx = xCurve * caveFrequency * caveHorizontalMultiplier;
        float fy = yCurve * caveFrequency * caveVerticalMultiplier;
        float fz = z * caveFrequency * 0.7f;

        float noiseXY = Mathf.PerlinNoise(fx, fy);
        float noiseYZ = Mathf.PerlinNoise(fy, fz);
        float noiseZX = Mathf.PerlinNoise(fz, fx);
        float noise = (noiseXY + noiseYZ + noiseZX) / 3f;

        if (secondaryFrequency > 0f && secondaryWeight > 0f)
        {
            float sfx = xCurve * secondaryFrequency * caveHorizontalMultiplier;
            float sfy = yCurve * secondaryFrequency * caveVerticalMultiplier;
            float sfz = z * secondaryFrequency * 0.7f;
            float snoiseXY = Mathf.PerlinNoise(sfx, sfy);
            float snoiseYZ = Mathf.PerlinNoise(sfy, sfz);
            float snoiseZX = Mathf.PerlinNoise(sfz, sfx);
            float snoise = (snoiseXY + snoiseYZ + snoiseZX) / 3f;
            noise = Mathf.Lerp(noise, snoise, secondaryWeight);
        }

        if (caveSharpness != 1f) noise = Mathf.Pow(noise, caveSharpness);

        if (verticalBiasStrength > 0f && verticalBias != 0f)
        {
            float yNorm = Mathf.InverseLerp(minY, caveStartY, y) * 2 - 1;
            float bias = 1f - Mathf.Abs(yNorm - verticalBias);
            noise = Mathf.Lerp(noise, noise * bias, verticalBiasStrength);
        }

        if (caveVerticalScale != 0f)
        {
            float yNormalized = Mathf.InverseLerp(minY, caveStartY, y);
            float scale = Mathf.Lerp(1f, 0.25f, yNormalized * caveVerticalScale);
            noise *= scale;
        }

        if (caveHorizontalScale != 0f)
        {
            float xNormalized = Mathf.InverseLerp(minX, maxX, x);
            float scale = Mathf.Lerp(1f, 0.25f, xNormalized * caveHorizontalScale);
            noise *= scale;
        }

        return noise;
    }

    // =============================
    //   FOG HANDLING
    // =============================

    public void DrawCavesFromArchive(ChunkedWorldArchive archive, int playerZ, float threshold = 0.5f)
    {
        if (archive == null || caveTilemapFog == null || fogDeleteArchive == null || playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        foreach (var pair in archive.AllTiles())
        {
            Vector3Int gridPos = pair.Key;
            TileData tileData = pair.Value;
            if (tileData == null) continue;
            if (tileData.blockTagOrName != "cave") continue;
            if (tileData.discovered) continue;
            if (fogDeleteArchive.IsDeleted(gridPos)) continue;
            if (gridPos.z != playerZ + 1) continue;

            Vector3 worldPos = caveTilemapFog.GetCellCenterWorld(gridPos);
            if (Vector3.Distance(worldPos, playerPos) <= 10.0f) continue;

            caveTilemapFog.SetTile(gridPos, visibleCaveTileAsset);

            // Archive this fog permanently
            fogDeleteArchive.MarkDeleted(gridPos);
            fogDeleteArchive.SaveToFile(System.IO.Path.Combine(Application.persistentDataPath, "deleted_fog.json"));

            if (debugFog) Debug.Log($"[FogSpawn] Archived fog spawn at {gridPos}");
        }
    }

    public void EnsureTileOnCorrectTilemap(Vector3Int gridPos, int playerZ)
    {
        if (caveTilemap == null || caveTilemapFog == null || caveTilemapHidden == null) return;

        caveTilemap.SetTile(gridPos, null);
        caveTilemapFog.SetTile(gridPos, null);
        caveTilemapHidden.SetTile(gridPos, null);

        if (gridPos.z == playerZ)
            caveTilemap.SetTile(gridPos, visibleCaveTileAsset2);
        else if (gridPos.z == playerZ + 1)
            caveTilemapFog.SetTile(gridPos, visibleCaveTileAsset);
        else if (gridPos.z == playerZ - 1)
            caveTilemapHidden.SetTile(gridPos, visibleCaveTileAsset);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = playerTransform != null ? playerTransform.position : Vector3.zero;
        float drawRadius = deleteCavesRadius;

        if (caveTilemap != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var pos in caveTilemap.cellBounds.allPositionsWithin)
            {
                if (caveTilemap.HasTile(pos))
                {
                    Vector3 worldPos = caveTilemap.GetCellCenterWorld(pos);
                    if (Vector3.Distance(worldPos, center) <= drawRadius)
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }
            }
        }
        if (caveTilemapFog != null)
        {
            Gizmos.color = Color.magenta;
            foreach (var pos in caveTilemapFog.cellBounds.allPositionsWithin)
            {
                if (caveTilemapFog.HasTile(pos))
                {
                    Vector3 worldPos = caveTilemapFog.GetCellCenterWorld(pos);
                    if (Vector3.Distance(worldPos, center) <= drawRadius)
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }
            }
        }
        if (caveTilemapHidden != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var pos in caveTilemapHidden.cellBounds.allPositionsWithin)
            {
                if (caveTilemapHidden.HasTile(pos))
                {
                    Vector3 worldPos = caveTilemapHidden.GetCellCenterWorld(pos);
                    if (Vector3.Distance(worldPos, center) <= drawRadius)
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.8f);
                }
            }
        }
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(center, drawRadius);
    }
}