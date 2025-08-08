using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInfiniteCameraSpawner : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private TileBase groundTileAsset;   // Dirt
    [SerializeField] private TileBase grassTileAsset;    // Grass
    [SerializeField] private Camera cam;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private int verticalDepth = 30;     // Depth (Y, negative)
    [SerializeField] private int buffer = 4;             // Extra tiles around view for smooth scrolling
    [SerializeField, Range(0f, 1f)] private float layerFraction = 0.25f; // 1/4 tile offset
    [SerializeField]
    private AnimationCurve hillCurve = new AnimationCurve(
        new Keyframe(-1, 1),
        new Keyframe(0, 0),
        new Keyframe(1, -1)
    );
    [SerializeField] private float hillScale = 0.06f; // How fast the hills change (smaller = slower hills)
    [SerializeField] private float hillHeight = 3f;   // Max amplitude of hills

    // --- RANDOMIZED HILLS ---
    [SerializeField] private float hillRandomSeed = 1337f;
    [SerializeField, Range(0f, 1f)] private float hillRandomAmplitude = 0.3f;

    private HashSet<Vector3Int> activeTiles = new HashSet<Vector3Int>();
    private Vector3Int previousPlayerPos;

    void Start()
    {
        previousPlayerPos = Vector3Int.FloorToInt(playerTransform.position);
    }

    float GetHillValue(int x, int z)
    {
        // Regular curve value
        float curveValue = hillCurve.Evaluate(x * hillScale);

        // Random noise value for extra variety
        float noiseValue = Mathf.PerlinNoise(
            x * hillScale + hillRandomSeed,
            z * hillScale + hillRandomSeed * 0.5f // slight variation by z
        );

        // noiseValue is in [0,1], center it to [-0.5,0.5]
        noiseValue -= 0.5f;

        // Mix curve and noise
        float finalValue = curveValue + (noiseValue * hillRandomAmplitude);

        return finalValue;
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        Vector3Int playerPos = Vector3Int.FloorToInt(playerTransform.position);
        int playerX = playerPos.x;
        int playerZ = playerPos.z;

        // Calculate camera bounds (in world coords)
        Vector3 camPos = cam.transform.position;
        float halfWidth = cam.orthographicSize * cam.aspect;
        float halfHeight = cam.orthographicSize;

        int minX = Mathf.FloorToInt(camPos.x - halfWidth) - buffer;
        int maxX = Mathf.CeilToInt(camPos.x + halfWidth) + buffer;
        int minZ = Mathf.FloorToInt(camPos.z - halfHeight) - buffer;
        int maxZ = Mathf.CeilToInt(camPos.z + halfHeight) + buffer;

        // Remove tiles outside camera bounds or Z layers more than 1 from player
        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var tile in activeTiles)
        {
            bool tooFarZ = Mathf.Abs(tile.z - playerZ) > 1;
            bool outOfView = (tile.x < minX || tile.x > maxX || tile.z < minZ || tile.z > maxZ);
            if (tooFarZ || outOfView)
            {
                groundTilemap.SetTile(tile, null);
                groundTilemap.SetTransformMatrix(tile, Matrix4x4.identity);
                toRemove.Add(tile);
            }
        }
        foreach (var tile in toRemove)
            activeTiles.Remove(tile);

        // Spawn visible tiles and offset the ones 1 in front and 1 behind
        for (int z = minZ; z <= maxZ; z++)
        {
            if (Mathf.Abs(z - playerZ) <= 1)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // --- HILL LOGIC WITH RANDOMNESS ---
                    float hillValue = GetHillValue(x, z);
                    int grassY = Mathf.RoundToInt(hillValue * hillHeight);

                    Vector3Int grassPos = new Vector3Int(x, grassY, z);
                    if (!activeTiles.Contains(grassPos))
                    {
                        groundTilemap.SetTile(grassPos, grassTileAsset);
                        activeTiles.Add(grassPos);

                        // Visual offset only for tiles 1 in front and 1 behind (initial spawn)
                        if (z == playerZ + 1)
                        {
                            Vector3 offset = new Vector3(0, -layerFraction, 0);
                            groundTilemap.SetTransformMatrix(grassPos, Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one));
                        }
                        else if (z == playerZ - 1)
                        {
                            Vector3 offset = new Vector3(0, layerFraction, 0);
                            groundTilemap.SetTransformMatrix(grassPos, Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one));
                        }
                        else
                        {
                            groundTilemap.SetTransformMatrix(grassPos, Matrix4x4.identity);
                        }

                        // Spawn dirt below, applying same offset
                        for (int y = grassY - 1; y >= grassY - verticalDepth; y--)
                        {
                            Vector3Int dirtPos = new Vector3Int(x, y, z);
                            if (!activeTiles.Contains(dirtPos))
                            {
                                groundTilemap.SetTile(dirtPos, groundTileAsset);
                                activeTiles.Add(dirtPos);

                                if (z == playerZ + 1)
                                {
                                    Vector3 dirtOffset = new Vector3(0, -layerFraction, 0);
                                    groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.TRS(dirtOffset, Quaternion.identity, Vector3.one));
                                }
                                else if (z == playerZ - 1)
                                {
                                    Vector3 dirtOffset = new Vector3(0, layerFraction, 0);
                                    groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.TRS(dirtOffset, Quaternion.identity, Vector3.one));
                                }
                                else
                                {
                                    groundTilemap.SetTransformMatrix(dirtPos, Matrix4x4.identity);
                                }
                            }
                        }
                    }
                }
            }
        }

        // --- NEW LOGIC: Apply offset every frame to visible layers, so it updates as the player moves ---
        for (int z = playerZ - 1; z <= playerZ + 1; z++)
        {
            float offsetY = 0f;
            if (z == playerZ + 1)
                offsetY = -layerFraction; // front, down
            else if (z == playerZ - 1)
                offsetY = layerFraction;  // behind, up
            else
                offsetY = 0f;             // player layer, flat

            for (int x = minX; x <= maxX; x++)
            {
                // --- HILL LOGIC WITH RANDOMNESS ---
                float hillValue = GetHillValue(x, z);
                int grassY = Mathf.RoundToInt(hillValue * hillHeight);

                for (int y = grassY; y >= grassY - verticalDepth; y--)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (activeTiles.Contains(pos))
                        groundTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(new Vector3(0, offsetY, 0), Quaternion.identity, Vector3.one));
                }
            }
        }

        // Update previous player position for next frame
        previousPlayerPos = playerPos;

        // Set collider only for the ground tile directly under the player
        foreach (var tile in activeTiles)
        {
            // --- HILL LOGIC WITH RANDOMNESS ---
            float hillValue = GetHillValue(tile.x, tile.z);
            int hillY = Mathf.RoundToInt(hillValue * hillHeight);

            if (tile.x == playerX && tile.z == playerZ && tile.y == hillY)
            {
                groundTilemap.SetColliderType(tile, Tile.ColliderType.Grid);
            }
            else
            {
                groundTilemap.SetColliderType(tile, Tile.ColliderType.None);
            }
        }
    }
}