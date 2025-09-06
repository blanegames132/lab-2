//using UnityEngine;
//using UnityEngine.Tilemaps;
//using System.Collections.Generic;

//public class PowerUpSpawner : MonoBehaviour
//{
//    public Tilemap groundTilemap;
//    public Tilemap frontTilemap;
//    public Tilemap middleFrontTilemap;
//    public Tilemap middleBackTilemap;
//    public Tilemap backTilemap;
//    public GameObject powerUpPrefab;
//    public Camera cam;
//    public TileInfiniteCameraSpawner spawner;

//    [Tooltip("Chance (0-1) to try spawn a power-up at edge per frame.")]
//    [Range(0, 1)]
//    public float spawnChancePerEdge = 0.05f;

//    private Tilemap[] allTilemaps;
//    private HashSet<Vector3Int> spawnedPositions = new HashSet<Vector3Int>();

//    void Start()
//    {
//        allTilemaps = new Tilemap[] { groundTilemap, frontTilemap, middleFrontTilemap, middleBackTilemap, backTilemap };
//        if (!cam) cam = Camera.main;
//        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
//    }

//    void Update()
//    {
//        if (Random.value > spawnChancePerEdge) return;
//        if (spawner == null || cam == null) return;

//        float halfWidth = cam.orthographicSize * cam.aspect;
//        float camX = cam.transform.position.x;

//        // Randomly choose left or right edge
//        bool spawnLeft = Random.value < 0.5f;
//        int x = Mathf.RoundToInt(spawnLeft ? camX - halfWidth : camX + halfWidth);

//        // Random Z layer (your 5 layers convention)
//        int camZ = Mathf.RoundToInt(cam.transform.position.z);
//        int[] zs = new int[] { camZ - 2, camZ - 1, camZ, camZ + 1, camZ + 2 };
//        int z = zs[Random.Range(0, zs.Length)];

//        // Always clamp to the surface using your terrain function
//        int surfaceY = spawner.GetSurfaceY(x, z);
//        Vector3Int tilePos = new Vector3Int(x, surfaceY, z);

//        if (spawnedPositions.Contains(tilePos)) return; // Prevent double-spawn

//        Tilemap chosenMap = allTilemaps[Random.Range(0, allTilemaps.Length)];
//        // SPAWN A LITTLE ABOVE THE GROUND (0.5 units up)
//        Vector3 worldSpawn = chosenMap.GetCellCenterWorld(tilePos) + Vector3.up * 0.5f;

//        Instantiate(powerUpPrefab, worldSpawn, Quaternion.identity);
//        spawnedPositions.Add(tilePos);
//    }
//}