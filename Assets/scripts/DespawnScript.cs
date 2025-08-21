using UnityEngine;

/// <summary>
/// Attach this to each enemy. It despawns itself if off-screen or out of z-range.
/// </summary>
public class DespawnScript : MonoBehaviour
{
    private EnemySpawner spawner;
    private Camera cam;
    private Transform playerTransform;
    private float yOffset = 1.1f;

    public void Initialize(EnemySpawner spawner, Camera cam, Transform playerTransform)
    {
        this.spawner = spawner;
        this.cam = cam;
        this.playerTransform = playerTransform;
    }

    void Update()
    {
        if (cam == null || playerTransform == null || spawner == null) return;

        float halfWidth = cam.orthographicSize * cam.aspect;
        float camX = cam.transform.position.x;
        float camLeft = camX - halfWidth;
        float camRight = camX + halfWidth;
        float playerZ = playerTransform.position.z;

        Vector3 pos = transform.position;

        bool offScreen = pos.x < camLeft || pos.x > camRight;
        bool zOutOfRange = pos.z < playerZ - 5f || pos.z > playerZ + 5f;

        // Despawn if either off-screen or out of z-range
        if (offScreen || zOutOfRange)
        {
            Vector3Int tilePos = new Vector3Int(
                Mathf.RoundToInt(pos.x),
                Mathf.RoundToInt(pos.y - yOffset),
                Mathf.RoundToInt(pos.z)
            );
            spawner.DespawnEnemy(gameObject, tilePos);
        }
    }
}