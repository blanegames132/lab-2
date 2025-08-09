using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

// --- ESSENTIAL WORLD GENERATION COMPONENT ---
public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [Header("Hill Randomization")]
    [SerializeField] public string hillRandomSeed = "";
    [SerializeField, Tooltip("The actual integer hash generated from hillRandomSeed. Changing this does nothing.")]
    private int generatedSeedHash;
    [SerializeField, Tooltip("The actual string seed used (random if blank at start).")]
    private string usedSeedString;

    [Header("Hill Shape Controls")]
    [SerializeField] private AnimationCurve hillCurve;
    [SerializeField] public float hillHeight;
    [SerializeField] private float cliffSharpness;
    [SerializeField] private float seedAmplitude;
    [SerializeField] private float seedScale;
    [SerializeField] private float hillNoiseScale;
    [SerializeField] private float hillCurveRandomJitter;
    [SerializeField] private float hillRandomAmplitude;
    [SerializeField] private float hillVerticalShift;

    [Header("Tilemap Setup")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap frontTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap backTilemap;
    [SerializeField] private TileBase groundTileAsset;
    [SerializeField] private TileBase grassTileAsset;
    [SerializeField] private TileBase hideTileAsset;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int buffer;

    [Header("Advanced Seed Controls")]
    [SerializeField] private float repeatRange;
    [SerializeField] private float curveShift;
    [SerializeField] private float perlinOffsetX;
    [SerializeField] private float perlinOffsetZ;
    [SerializeField] private float perlinStrength;
    [SerializeField] private float perlinBase;

    [Header("World Controls")]
    [SerializeField] private int worldBottomY;

    // --- INTERNAL STATE ---
    private AnimationCurve randomHillCurve;
    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> hiddenTiles = new HashSet<Vector3Int>();

    void Awake()
    {
        if (string.IsNullOrEmpty(hillRandomSeed) || hillRandomSeed.ToLower() == "random")
        {
            hillRandomSeed = DateTime.Now.Ticks.ToString();
        }
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
    }

    void OnValidate()
    {
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
    }

    [ContextMenu("Recalculate Seed Hash")]
    public void RecalculateSeedHash()
    {
        usedSeedString = hillRandomSeed;
        ApplySeedRandomization();
    }

    public int GetSurfaceY(int x, int z)
    {
        float hillValue = GetHillValue(x, z);
        int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);
        return surfaceY;
    }

    int HashSeed(string seed)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }

    float SeededValue(System.Random rand, float min, float max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return min + ((float)rand.NextDouble() * (max - min));
    }
    int SeededInt(System.Random rand, int min, int max, int offset)
    {
        rand = new System.Random(rand.Next() + offset);
        return rand.Next(min, max);
    }

    void ApplySeedRandomization()
    {
        int hash = HashSeed(usedSeedString);
        generatedSeedHash = hash;

        System.Random rand = new System.Random(hash);

        seedScale = SeededValue(rand, 0.05f, 0.2f, 1);
        seedAmplitude = SeededValue(rand, 0.6f, 3.0f, 2);
        repeatRange = SeededValue(rand, 8f, 28f, 3);
        hillHeight = SeededValue(rand, 3f, 15f, 4);
        hillCurveRandomJitter = SeededValue(rand, 0.08f, 0.7f, 5);
        hillRandomAmplitude = SeededValue(rand, 0.05f, 0.9f, 6);
        hillNoiseScale = SeededValue(rand, 0.25f, 1.3f, 7);
        curveShift = SeededValue(rand, -1.2f, 1.2f, 8);
        perlinOffsetX = SeededValue(rand, 0f, 100f, 9);
        perlinOffsetZ = SeededValue(rand, 0f, 100f, 10);
        perlinStrength = SeededValue(rand, 0.3f, 1.6f, 11);
        perlinBase = SeededValue(rand, 0f, 1.0f, 12);
        hillVerticalShift = SeededValue(rand, -2f, 2f, 13);
        cliffSharpness = SeededValue(rand, 1.5f, 3.0f, 14);
        buffer = SeededInt(rand, 2, 6, 15);
        worldBottomY = SeededInt(rand, -600, -200, 16);

        randomHillCurve = new AnimationCurve();
        int numKeys = SeededInt(rand, 4, 8, 17);
        for (int i = 0; i < numKeys; i++)
        {
            float t = Mathf.Lerp(-1f, 1f, (float)i / (numKeys - 1));
            float baseValue = Mathf.Sin(t * Mathf.PI * SeededValue(rand, 1f, 2f, 20 + i));
            float value = baseValue * cliffSharpness + SeededValue(rand, -hillCurveRandomJitter, hillCurveRandomJitter, 100 + i);
            randomHillCurve.AddKey(new Keyframe(
                t + curveShift * ((float)i / numKeys),
                Mathf.Clamp(value, -cliffSharpness, cliffSharpness)
            ));
        }
    }

    public float GetHillValue(int x, int z)
    {
        float layerOffset = Mathf.PerlinNoise(z * hillNoiseScale + perlinOffsetZ, curveShift * 0.29f) * 2f - 1f;
        float t = x * seedScale + curveShift + z * seedScale * 0.11f + layerOffset * hillRandomAmplitude * 2f;
        float tCurve = Mathf.Repeat(t, repeatRange) / (repeatRange / 2f) - 1f;
        float curveValue = randomHillCurve.Evaluate(tCurve) * seedAmplitude;

        float noiseValue = Mathf.PerlinNoise(x * hillNoiseScale + perlinOffsetX, z * hillNoiseScale + perlinOffsetZ);
        float extraNoise = Mathf.PerlinNoise(x * hillNoiseScale * 0.2f + perlinBase, z * hillNoiseScale * 0.2f + perlinBase) - 0.5f;
        noiseValue = (noiseValue - 0.5f) * perlinStrength + extraNoise * (hillRandomAmplitude * 0.7f);
        noiseValue = Mathf.Sign(noiseValue) * Mathf.Pow(Mathf.Abs(noiseValue), cliffSharpness);

        float finalValue = curveValue + (noiseValue * hillRandomAmplitude) + hillVerticalShift;
        return finalValue;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (playerTransform == null) return;
        if (groundTilemap == null || frontTilemap == null || middleFrontTilemap == null || middleBackTilemap == null || backTilemap == null || groundTileAsset == null || grassTileAsset == null) return;

        Vector3Int playerPos = Vector3Int.FloorToInt(playerTransform.position);
        int playerZ = playerPos.z;
        int playerY = Mathf.FloorToInt(playerTransform.position.y);

        Vector3 camPos = cam.transform.position;
        float halfWidth = cam.orthographicSize * cam.aspect;
        int minX = Mathf.FloorToInt(camPos.x - halfWidth) - buffer;
        int maxX = Mathf.CeilToInt(camPos.x + halfWidth) + buffer;

        float camY = cam.transform.position.y;
        float halfCamHeight = cam.orthographicSize;
        int minY = Mathf.FloorToInt(camY - halfCamHeight) - buffer;
        int maxY = Mathf.CeilToInt(camY + halfCamHeight) + buffer;
        int buildBottom = Mathf.Min(minY, playerY);

        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var tile in activeTiles)
        {
            bool outOfView = (tile.x < minX || tile.x > maxX || tile.z < playerZ - 2 || tile.z > playerZ + 2);
            bool belowWorldBottom = tile.y < worldBottomY;
            if (outOfView || belowWorldBottom)
            {
                groundTilemap.SetTile(tile, null);
                groundTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                frontTilemap.SetTile(tile, null);
                frontTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                middleFrontTilemap.SetTile(tile, null);
                middleFrontTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                middleBackTilemap.SetTile(tile, null);
                middleBackTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                backTilemap.SetTile(tile, null);
                backTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                toRemove.Add(tile);
            }
        }
        foreach (var tile in toRemove)
            activeTiles.Remove(tile);

        int[] zs = new int[] { playerZ - 2, playerZ - 1, playerZ, playerZ + 1, playerZ + 2 };

        for (int z_i = 0; z_i < zs.Length; z_i++)
        {
            int z = zs[z_i];
            for (int x = minX; x <= maxX; x++)
            {
                float hillValue = GetHillValue(x, z);
                int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

                Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);

                groundTilemap.SetTile(surfacePos, null);
                groundTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                frontTilemap.SetTile(surfacePos, null);
                frontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                middleFrontTilemap.SetTile(surfacePos, null);
                middleFrontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                middleBackTilemap.SetTile(surfacePos, null);
                middleBackTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                backTilemap.SetTile(surfacePos, null);
                backTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);

                if (z == playerZ - 2)
                {
                    frontTilemap.SetTile(surfacePos, grassTileAsset);
                    frontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                }
                else if (z == playerZ - 1)
                {
                    middleFrontTilemap.SetTile(surfacePos, grassTileAsset);
                    middleFrontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                }
                else if (z == playerZ)
                {
                    groundTilemap.SetTile(surfacePos, grassTileAsset);
                    groundTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                }
                else if (z == playerZ + 1)
                {
                    middleBackTilemap.SetTile(surfacePos, grassTileAsset);
                    middleBackTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                }
                else if (z == playerZ + 2)
                {
                    backTilemap.SetTile(surfacePos, grassTileAsset);
                    backTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                }
                activeTiles.Add(surfacePos);

                for (int y = surfaceY - 1; y >= buildBottom; y--)
                {
                    Vector3Int dirtPos = new Vector3Int(x, y, z);

                    groundTilemap.SetTile(dirtPos, null);
                    groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    frontTilemap.SetTile(dirtPos, null);
                    frontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    middleFrontTilemap.SetTile(dirtPos, null);
                    middleFrontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    middleBackTilemap.SetTile(dirtPos, null);
                    middleBackTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    backTilemap.SetTile(dirtPos, null);
                    backTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);

                    if (z == playerZ - 2)
                    {
                        frontTilemap.SetTile(dirtPos, groundTileAsset);
                        frontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    }
                    else if (z == playerZ - 1)
                    {
                        middleFrontTilemap.SetTile(dirtPos, groundTileAsset);
                        middleFrontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    }
                    else if (z == playerZ)
                    {
                        groundTilemap.SetTile(dirtPos, groundTileAsset);
                        groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    }
                    else if (z == playerZ + 1)
                    {
                        middleBackTilemap.SetTile(dirtPos, groundTileAsset);
                        middleBackTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    }
                    else if (z == playerZ + 2)
                    {
                        backTilemap.SetTile(dirtPos, groundTileAsset);
                        backTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                    }
                    activeTiles.Add(dirtPos);
                }
            }
        }

        // --- COLLIDER SETUP: Only the main ground layer (playerZ) gets colliders ---
        foreach (var tile in activeTiles)
        {
            if (tile.z == playerZ)
            {
                groundTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            }
            else
            {
                groundTilemap.SetColliderType(tile, Tile.ColliderType.None);
            }
            middleFrontTilemap.SetColliderType(tile, Tile.ColliderType.None);
            middleBackTilemap.SetColliderType(tile, Tile.ColliderType.None);
            frontTilemap.SetColliderType(tile, Tile.ColliderType.None);
            backTilemap.SetColliderType(tile, Tile.ColliderType.None);
        }

        HideHalfCircleBelowPlayer(playerTransform.position, 4);
    }

    // --- ESSENTIAL: HIDE TILES BELOW PLAYER ---
    private void HideHalfCircleBelowPlayer(Vector3 playerWorldPos, int radius)
    {
        Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(playerWorldPos.x), Mathf.RoundToInt(playerWorldPos.y));
        int playerZ = Mathf.FloorToInt(playerWorldPos.z);

        int[] layerZs = new int[] { playerZ - 2, playerZ - 1, playerZ, playerZ + 1, playerZ + 2 };

        HashSet<Vector3Int> currentlyHidden = new HashSet<Vector3Int>();

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= 0; dy++)
            {
                if (dx * dx + dy * dy > radius * radius) continue;
                Vector2Int offset = new Vector2Int(dx, dy);
                Vector2Int tileXY = playerXY + offset;
                foreach (int z in layerZs)
                {
                    Vector3Int pos = new Vector3Int(tileXY.x, tileXY.y, z);
                    if (tileXY == playerXY && z == playerZ) continue;

                    // Hide the tile by replacing it with the hideTileAsset
                    // Only on the front and middleFront layers (adjust as needed)
                    frontTilemap.SetTile(pos, hideTileAsset);
                    frontTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                    frontTilemap.SetColliderType(pos, Tile.ColliderType.None);

                    middleFrontTilemap.SetTile(pos, hideTileAsset);
                    middleFrontTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                    middleFrontTilemap.SetColliderType(pos, Tile.ColliderType.None);

                    currentlyHidden.Add(pos);
                }
            }
        }

        // Unhide any previously hidden tiles not in the current set
        foreach (var pos in hiddenTiles)
        {
            if (!currentlyHidden.Contains(pos))
            {
                frontTilemap.SetTile(pos, null);
                frontTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                frontTilemap.SetColliderType(pos, Tile.ColliderType.None);

                middleFrontTilemap.SetTile(pos, null);
                middleFrontTilemap.SetTransformMatrix(pos, Matrix4x4.identity);
                middleFrontTilemap.SetColliderType(pos, Tile.ColliderType.None);
            }
        }

        hiddenTiles = currentlyHidden;
    }

    // --- ESSENTIAL: FOR PLAYER PATH CHECKING ---
    public bool IsTileHidden(Vector3Int pos)
    {
        return hiddenTiles.Contains(pos);
    }
}