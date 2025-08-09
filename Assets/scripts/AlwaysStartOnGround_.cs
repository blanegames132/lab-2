using UnityEngine;

public class AlwaysStartOnGround : MonoBehaviour
{
    [SerializeField] private TileInfiniteCameraSpawner spawner;

    void Start()
    {
        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        ClampToNearestGroundAtStart();
    }

    void ClampToNearestGroundAtStart()
    {
        if (spawner == null)
        {
            Debug.LogWarning("No TileInfiniteCameraSpawner found!");
            return;
        }
        Vector3 pos = transform.position;
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);

        int surfaceY = spawner.GetSurfaceY(x, z);

        pos.y = surfaceY + 1.1f;
        pos.z = z;
        transform.position = pos;
        Debug.Log($"Player clamped to ground at {pos}");
    }
}