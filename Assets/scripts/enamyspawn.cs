using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Camera cam;
    [SerializeField] private TileInfiniteCameraSpawner spawner;

    [Tooltip("Chance (0-1) to try spawn an enemy at edge per frame.")]
    [Range(0, 1)]
    public float spawnChancePerEdge = 0.05f;

    [Tooltip("Maximum number of active enemies.")]
    public int maxEnemies = 2;

    private HashSet<Vector3Int> spawnedPositions = new HashSet<Vector3Int>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private Transform playerTransform;

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        RemoveNullEnemies();

        if (activeEnemies.Count >= maxEnemies) return;
        if (Random.value > spawnChancePerEdge) return;
        if (spawner == null || cam == null || enemyPrefab == null) return;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float camX = cam.transform.position.x;

        bool spawnLeft = Random.value < 0.5f;
        int x = Mathf.RoundToInt(spawnLeft ? camX - halfWidth : camX + halfWidth);

        int camZ = Mathf.RoundToInt(cam.transform.position.z);
        int[] zs = new int[] { camZ - 2, camZ - 1, camZ, camZ + 1, camZ + 2 };
        int z = zs[Random.Range(0, zs.Length)];

        int surfaceY = spawner.GetSurfaceY(x, z);
        Vector3Int tilePos = new Vector3Int(x, surfaceY, z);

        if (spawnedPositions.Contains(tilePos)) return;

        Vector3 worldSpawn = new Vector3(x, surfaceY + 1.1f, z);

        GameObject enemy = Instantiate(enemyPrefab, worldSpawn, Quaternion.identity);
        activeEnemies.Add(enemy);
        spawnedPositions.Add(tilePos);

        // Attach DespawnScript for automatic despawn
        DespawnScript ds = enemy.AddComponent<DespawnScript>();
        ds.Initialize(this, cam, playerTransform);
    }

    // Removes null enemies from the activeEnemies list
    void RemoveNullEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
        }
    }

    // Called by DespawnScript when an enemy should be despawned
    public void DespawnEnemy(GameObject enemy, Vector3Int tilePos)
    {
        spawnedPositions.Remove(tilePos);
        activeEnemies.Remove(enemy);
        Destroy(enemy);
    }
}