//using UnityEngine;
//using System.Collections.Generic;

///// <summary>
///// Spawns enemies in two bands (left/right) around the player, within a specific Z band.
///// Despawns enemies when they leave the extended X range or Z band.
///// </summary>
//public class EnemySpawner : MonoBehaviour
//{
//    [Header("References")]
//    [SerializeField] private GameObject enemyPrefab;
//    [SerializeField] private TileInfiniteCameraSpawner spawner;
//    [SerializeField] private Transform playerTransform;

//    [Header("Spawn Settings")]
//    [Range(0, 1)] public float spawnChance = 0.05f;
//    public int maxEnemies = 4;

//    [Header("Spawn Band (relative to player)")]
//    public float spawnZBand = 2f; // +/- Z band around player
//    public float spawnLeftMinX = -20f;
//    public float spawnLeftMaxX = -4f;
//    public float spawnRightMinX = 4f;
//    public float spawnRightMaxX = 20f;

//    [Header("Despawn Band (relative to player)")]
//    public float despawnLeftAbsX = -30f;
//    public float despawnLeftTimerX = -20f;
//    public float despawnRightTimerX = 20f;
//    public float despawnRightAbsX = 30f;
//    public float despawnZBand = 2f; // +/- Z band for despawn (absolute)
//    public float despawnTimer = 20f;

//    private List<GameObject> activeEnemies = new List<GameObject>();

//    void Start()
//    {
//        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
//        if (!playerTransform)
//        {
//            GameObject player = GameObject.FindGameObjectWithTag("Player");
//            if (player != null) playerTransform = player.transform;
//        }
//    }

//    void Update()
//    {
//        RemoveNullEnemies();

//        if (activeEnemies.Count >= maxEnemies) return;
//        if (Random.value > spawnChance) return;
//        if (!spawner || !enemyPrefab || !playerTransform) return;

//        // Choose side: 0 = left, 1 = right
//        bool spawnLeft = Random.value < 0.5f;
//        float px = playerTransform.position.x;
//        float pz = playerTransform.position.z;

//        float x = spawnLeft
//            ? Random.Range(px + spawnLeftMinX, px + spawnLeftMaxX)
//            : Random.Range(px + spawnRightMinX, px + spawnRightMaxX);

//        float z = Random.Range(pz - spawnZBand, pz + spawnZBand);

//        int ix = Mathf.RoundToInt(x);
//        int iz = Mathf.RoundToInt(z);

//        int surfaceY = spawner.GetSurfaceY(ix, iz);

//        Vector3 worldSpawn = new Vector3(ix, surfaceY + 1.1f, iz);

//        GameObject enemy = Instantiate(enemyPrefab, worldSpawn, Quaternion.identity);
//        activeEnemies.Add(enemy);

//        // Attach despawn script with ALL required arguments
//        DespawnScript ds = enemy.AddComponent<DespawnScript>();
//        ds.Initialize(
//            this,
//            playerTransform,
//            despawnLeftAbsX,
//            despawnLeftTimerX,
//            despawnRightTimerX,
//            despawnRightAbsX,
//            despawnZBand,
//            despawnTimer
//        );
//    }

//    void RemoveNullEnemies()
//    {
//        for (int i = activeEnemies.Count - 1; i >= 0; i--)
//            if (activeEnemies[i] == null) activeEnemies.RemoveAt(i);
//    }

//    public void DespawnEnemy(GameObject enemy)
//    {
//        activeEnemies.Remove(enemy);
//        Destroy(enemy);
//    }
//}