using UnityEngine;

public class camera : MonoBehaviour
{
    [SerializeField] private float offsetDistance = 2f;  // How far ahead/behind the camera moves
    [SerializeField] private float smoothSpeed = 5f;     // Camera follow speed
    [SerializeField] private float deadZone = 0.05f;     // Ignore tiny velocity to avoid jitter
    [SerializeField] private float zoomSpeed = 5f;       // How fast the zoom changes
    [SerializeField] private float minZoom = 5f;         // Minimum orthographic size (zoomed in)
    [SerializeField] private float maxZoom = 20f;        // Maximum orthographic size (zoomed out)

    void Start()
    {
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void LateUpdate()
    {
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            Vector3 playerPosition = player.transform.position;
            Vector3 cameraPosition = Camera.main.transform.position;

            if (rb.linearVelocity.x > deadZone)
            {
                cameraPosition.x = playerPosition.x + offsetDistance;
            }
            else if (rb.linearVelocity.x < -deadZone)
            {
                cameraPosition.x = playerPosition.x - offsetDistance;
            }
            else
            {
                cameraPosition.x = playerPosition.x;
            }

            cameraPosition.y = playerPosition.y;

            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                cameraPosition,
                Time.deltaTime * smoothSpeed
            );
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            float targetZoom = Camera.main.orthographicSize - scroll * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }
    }
}