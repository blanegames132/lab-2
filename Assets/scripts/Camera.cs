using UnityEngine;

public class camera : MonoBehaviour
{
    [SerializeField] private float offsetDistance = 2f;  // How far ahead/behind the camera moves
    [SerializeField] private float smoothSpeed = 5f;     // Camera follow speed
    [SerializeField] private float deadZone = 0.05f;     // Ignore tiny velocity to avoid jitter

    void Start()
    {
        // Ensure camera rotation is reset
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void LateUpdate() // Use LateUpdate to reduce camera jitter
    {
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            Vector3 playerPosition = player.transform.position;
            Vector3 cameraPosition = Camera.main.transform.position;

            // Determine horizontal camera offset based on player velocity
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

            // Always follow player's Y position
            cameraPosition.y = playerPosition.y;

            // Smoothly move the camera towards the target position
            Camera.main.transform.position = Vector3.Lerp(
                Camera.main.transform.position,
                cameraPosition,
                Time.deltaTime * smoothSpeed
            );
        }
    }
}