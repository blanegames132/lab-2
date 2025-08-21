using UnityEngine;
using UnityEngine.Tilemaps;

public class PowerUpAutoDestroy : MonoBehaviour
{
    public Transform player;
    public float maxDistanceX = 30f;
    public float maxDistanceY = 20f;
    public float destroyAfterZSeconds = 300f; // 5 minutes
    public float zThreshold = 5f; // Destroy if farther than this in Z

    private float zTimer = 0f;

    void Start()
    {
        // Attempt to auto-assign player if not set
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null)
                player = found.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        Vector3 powerupPos = transform.position;
        Vector3 playerPos = player.position;

        // Destroy if too far in X or Y
        if (Mathf.Abs(powerupPos.x - playerPos.x) > maxDistanceX || Mathf.Abs(powerupPos.y - playerPos.y) > maxDistanceY)
        {
            Destroy(gameObject);
            return;
        }

        // Start/continue Z timer if > zThreshold units from player in Z, else reset
        if (Mathf.Abs(powerupPos.z - playerPos.z) > zThreshold)
        {
            zTimer += Time.deltaTime;
            if (zTimer >= destroyAfterZSeconds)
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            zTimer = 0f;
        }
    }
}