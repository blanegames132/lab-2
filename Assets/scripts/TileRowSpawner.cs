using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Security.Cryptography;
using System.Text;
using System;

public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [Header("Hill Randomization")]
    public string hillRandomSeed = "";
    [SerializeField, Range(0f, 1f)] private float hillCurveRandomJitter = 0.3f;
    [SerializeField, Range(0f, 1f)] private float hillRandomAmplitude = 0.3f;
    [SerializeField, Range(0.01f, 1f)] private float baseHillScale = 0.06f;
    [SerializeField, Tooltip("The actual integer hash generated from hillRandomSeed. Changing this does nothing.")]
    private int generatedSeedHash;
    [SerializeField, Tooltip("The actual string seed used (random if blank at start).")]
    private string usedSeedString;

    [Header("Hill Shape")]
    [SerializeField]
    private AnimationCurve hillCurve = new AnimationCurve(
        new Keyframe(-1, 1),
        new Keyframe(-0.5f, 0.5f),
        new Keyframe(0, 0),
        new Keyframe(0.5f, -0.5f),
        new Keyframe(1, -1)
    );
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap frontTilemap;
    [SerializeField] private TileBase groundTileAsset;
    [SerializeField] private TileBase grassTileAsset;
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int buffer = 4;
    [SerializeField, Range(0f, 1f)] private float layerFraction = 0.25f;
    [SerializeField] private float hillHeight = 3f;

    private AnimationCurve randomHillCurve;
    private float seedPhase = 0f;
    private float seedAmplitude = 1f;
    private float seedScale = 0.06f;
    private float repeatRange = 8f;
    private int worldBottomY = -500;

    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();

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

    void ApplySeedRandomization()
    {
        int hash = HashSeed(usedSeedString);
        generatedSeedHash = hash;

        System.Random rand = new System.Random(hash);

        seedPhase = (float)rand.NextDouble() * repeatRange - (repeatRange / 2f);
        seedAmplitude = 0.7f + (float)rand.NextDouble() * 0.9f;
        seedScale = baseHillScale * (0.6f + (float)rand.NextDouble() * 2.2f);
        repeatRange = 8f + (float)rand.NextDouble() * 12f;

        randomHillCurve = new AnimationCurve();
        for (int i = 0; i < hillCurve.length; i++)
        {
            Keyframe k = hillCurve[i];
            float jitter = ((float)rand.NextDouble() * 2.0f - 1.0f) * hillCurveRandomJitter;
            randomHillCurve.AddKey(new Keyframe(k.time, Mathf.Clamp(k.value + jitter, -1f, 1f)));
        }
    }

    int HashSeed(string seed)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
            return BitConverter.ToInt32(hashBytes, 0);
        }
    }

    float GetHillValue(int x, int z)
    {
        float layerOffset = Mathf.PerlinNoise(z * 0.1f + seedPhase * 0.13f, seedPhase * 0.29f) * 2f - 1f;
        float t = x * seedScale + seedPhase + z * seedScale * 0.11f + layerOffset * hillRandomAmplitude * 2f;
        float tCurve = Mathf.Repeat(t, repeatRange) / (repeatRange / 2f) - 1f;
        float curveValue = randomHillCurve.Evaluate(tCurve) * seedAmplitude;
        float noiseValue = Mathf.PerlinNoise(x * seedScale + seedPhase, z * seedScale + seedPhase * 0.5f);
        noiseValue -= 0.5f;
        float finalValue = curveValue + (noiseValue * hillRandomAmplitude);
        return finalValue;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        if (playerTransform == null) return;
        if (groundTilemap == null || frontTilemap == null || groundTileAsset == null || grassTileAsset == null) return;

        Vector3Int playerPos = Vector3Int.FloorToInt(playerTransform.position);

        Vector3 camPos = cam.transform.position;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float halfHeight = cam.orthographicSize;

        int minX = Mathf.FloorToInt(camPos.x - halfWidth) - buffer;
        int maxX = Mathf.CeilToInt(camPos.x + halfWidth) + buffer;
        int minZ = Mathf.FloorToInt(camPos.z - halfHeight) - buffer;
        int maxZ = Mathf.CeilToInt(camPos.z + halfHeight) + buffer;

        float camY = cam.transform.position.y;
        float halfCamHeight = cam.orthographicSize;
        int minY = Mathf.FloorToInt(camY - halfCamHeight) - buffer;
        int maxY = Mathf.CeilToInt(camY + halfCamHeight) + buffer;
        int playerY = Mathf.FloorToInt(playerTransform.position.y);
        int buildBottom = Mathf.Min(minY, playerY);

        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var tile in activeTiles)
        {
            bool outOfView = (tile.x < minX || tile.x > maxX || tile.z < minZ || tile.z > maxZ);
            bool belowWorldBottom = tile.y < worldBottomY;
            if (outOfView || belowWorldBottom)
            {
                groundTilemap.SetTile(tile, null);
                groundTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                if (frontTilemap.GetTile(tile) != null)
                {
                    frontTilemap.SetTile(tile, null);
                    frontTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                }
                toRemove.Add(tile);
            }
        }
        foreach (var tile in toRemove)
            activeTiles.Remove(tile);

        var surfaceTiles = new HashSet<Vector3Int>();
        int playerZ = playerPos.z;

        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float hillValue = GetHillValue(x, z);
                int surfaceY = Mathf.RoundToInt(hillValue * hillHeight);

                Vector3Int surfacePos = new Vector3Int(x, surfaceY, z);
                surfaceTiles.Add(surfacePos);

                int zDistance = Mathf.Abs(z - playerZ);
                bool shouldBeGround = (zDistance <= 1);
                bool shouldBeFront = (zDistance == 2);

                // Assign surface tiles
                if (shouldBeGround)
                {
                    groundTilemap.SetTile(surfacePos, grassTileAsset);
                    groundTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    if (frontTilemap.GetTile(surfacePos) != null)
                    {
                        frontTilemap.SetTile(surfacePos, null);
                        frontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    }
                    activeTiles.Add(surfacePos);
                }
                else if (shouldBeFront)
                {
                    frontTilemap.SetTile(surfacePos, grassTileAsset);
                    frontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    if (groundTilemap.GetTile(surfacePos) != null)
                    {
                        groundTilemap.SetTile(surfacePos, null);
                        groundTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    }
                    activeTiles.Add(surfacePos);
                }
                else
                {
                    groundTilemap.SetTile(surfacePos, null);
                    groundTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    if (frontTilemap.GetTile(surfacePos) != null)
                    {
                        frontTilemap.SetTile(surfacePos, null);
                        frontTilemap.SetTransformMatrix(surfacePos, Matrix4x4.identity);
                    }
                    activeTiles.Remove(surfacePos);
                }

                // Fill dirt below surface
                for (int y = surfaceY - 1; y >= buildBottom; y--)
                {
                    Vector3Int dirtPos = new Vector3Int(x, y, z);
                    if (shouldBeGround)
                    {
                        groundTilemap.SetTile(dirtPos, groundTileAsset);
                        groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        if (frontTilemap.GetTile(dirtPos) != null)
                        {
                            frontTilemap.SetTile(dirtPos, null);
                            frontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        }
                        activeTiles.Add(dirtPos);
                    }
                    else if (shouldBeFront)
                    {
                        frontTilemap.SetTile(dirtPos, groundTileAsset);
                        frontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        if (groundTilemap.GetTile(dirtPos) != null)
                        {
                            groundTilemap.SetTile(dirtPos, null);
                            groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        }
                        activeTiles.Add(dirtPos);
                    }
                    else
                    {
                        groundTilemap.SetTile(dirtPos, null);
                        groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        if (frontTilemap.GetTile(dirtPos) != null)
                        {
                            frontTilemap.SetTile(dirtPos, null);
                            frontTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                        }
                        activeTiles.Remove(dirtPos);
                    }
                }
            }
        }

        // Colliders for all surface tiles: only set collider for the player's Z layer
        foreach (var tile in activeTiles)
        {
            if (surfaceTiles.Contains(tile) && tile.z == playerPos.z)
                groundTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            else
                groundTilemap.SetColliderType(tile, Tile.ColliderType.None);
        }
    }
}