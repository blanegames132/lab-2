//using UnityEngine;

///// <summary>
///// Despawns enemy if outside absolute X or Z points (immediate), or after a timer if within the despawn band.
///// </summary>
//public class DespawnScript : MonoBehaviour
//{
//    private EnemySpawner spawner;
//    private Transform playerTransform;

//    // Inspector-editable despawn bands (relative to player)
//    public float despawnLeftAbsX = -30f;
//    public float despawnLeftTimerX = -20f;
//    public float despawnRightTimerX = 20f;
//    public float despawnRightAbsX = 30f;
//    public float despawnZBand = 2f;
//    public float despawnTimer = 20f;

//    private float timer = 0f;
//    private bool inTimerZone = false;

//    public void Initialize(
//        //EnemySpawner spawner,
//        Transform playerTransform,
//        float despawnLeftAbsX,
//        float despawnLeftTimerX,
//        float despawnRightTimerX,
//        float despawnRightAbsX,
//        float despawnZBand,
//        float despawnTimer)
//    {
//        this.spawner = spawner;
//        this.playerTransform = playerTransform;
//        this.despawnLeftAbsX = despawnLeftAbsX;
//        this.despawnLeftTimerX = despawnLeftTimerX;
//        this.despawnRightTimerX = despawnRightTimerX;
//        this.despawnRightAbsX = despawnRightAbsX;
//        this.despawnZBand = despawnZBand;
//        this.despawnTimer = despawnTimer;
//    }

//    void Update()
//    {
//        if (!playerTransform || !spawner) return;

//        float px = playerTransform.position.x;
//        float pz = playerTransform.position.z;
//        float ex = transform.position.x;
//        float ez = transform.position.z;

//        float relX = ex - px;
//        float relZ = ez - pz;

//        // ABSOLUTE DESPAWN: outside the outer X bounds or Z band
//        if (relX < despawnLeftAbsX || relX > despawnRightAbsX || Mathf.Abs(relZ) > despawnZBand)
//        {
//            spawner.DespawnEnemy(gameObject);
//            return;
//        }

//        // TIMER ZONE: if in between timer despawn points (on X)
//        if (relX >= despawnLeftTimerX && relX <= despawnRightTimerX)
//        {
//            inTimerZone = true;
//            timer += Time.deltaTime;
//            if (timer >= despawnTimer)
//            {
//                spawner.DespawnEnemy(gameObject);
//            }
//        }
//        else
//        {
//            // Not in timer zone, reset timer and despawn immediately
//            if (inTimerZone)
//            {
//                inTimerZone = false;
//                timer = 0f;
//            }
//            spawner.DespawnEnemy(gameObject);
//        }
//    }
//}