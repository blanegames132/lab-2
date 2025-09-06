using UnityEngine;

public class PlayerSpawnScript : MonoBehaviour
{
    [Header("World Spawn Bounds")]
    public float minRange = -10000f;
    public float maxRange = 10000f;

    public Transform playerTransform;

    // Always spawn at y = 1000
    public float spawnY = 1000f;

    void Awake()
    {
        if (playerTransform == null)
            playerTransform = transform;
    }

    public void RespawnPlayerAtRandom()
    {
        float x = Random.Range(minRange, maxRange);
        float z = Random.Range(minRange, maxRange);

        // Set Y to 1000 always
        float y = spawnY;

        playerTransform.position = new Vector3(x, y, z);


        // Reset velocity for Rigidbody2D if present
        var rb = playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void Start()
    {
        RespawnPlayerAtRandom();
    }
}