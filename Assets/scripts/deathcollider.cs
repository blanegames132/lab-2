using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DeathCollider : MonoBehaviour
{
    [Header("Death Collider Settings")]
    [Tooltip("Vertical position (Y) of the death collider. Always stays at this value.")]
    [SerializeField] private float deathColliderY = -500f;

    [Tooltip("Depth position (Z) of the death collider. Adjust if needed.")]
    [SerializeField] private float deathColliderZ = 0f;

    [Tooltip("Should the collider width match the camera view?")]
    [SerializeField] private bool useCameraWidth = true;

    [Tooltip("Manual width for the collider (ignored if Use Camera Width is enabled).")]
    [SerializeField] private float manualWidth = 20f;

    private float camWidth;

    void Start()
    {
        // Get camera width in world units if needed
        if (useCameraWidth)
        {
            Camera cam = Camera.main;
            float camHeight = cam.orthographicSize * 2f;
            camWidth = camHeight * cam.aspect;
        }
        else
        {
            camWidth = manualWidth;
        }

        // Resize collider to match width
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(camWidth, box.size.y);
        }

        // Set initial position
        Vector3 startPos = transform.position;
        startPos.y = deathColliderY;
        startPos.z = deathColliderZ;
        transform.position = startPos;
    }

    void Update()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Calculate camera horizontal bounds
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;
        float halfWidth = camWidth / 2f;

        float camCenterX = cam.transform.position.x;
        float minX = camCenterX - halfWidth;
        float maxX = camCenterX + halfWidth;

        // Clamp collider's X position to camera bounds
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = deathColliderY;
        pos.z = deathColliderZ;
        transform.position = pos;
    }

    // Optional: Draw a gizmo in the Scene view for visual aid
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(-1000, deathColliderY, deathColliderZ),
            new Vector3(1000, deathColliderY, deathColliderZ)
        );
    }
}