using UnityEngine;

/// <summary>
/// Controls parallax scrolling effect for background elements that follow the camera
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private float yPosOffset = 0.0f;
    [SerializeField] private float parallaxSpeed = 0.5f;
    [SerializeField] private bool debugMode = false;

    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private Material material;
    private Vector2 lastCameraPosition;
    private Vector2 textureOffset = Vector2.zero;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        // Cache the material and set initial texture
        material = spriteRenderer.material;
        material.SetTexture("_MainTex", spriteRenderer.sprite.texture);

        // Initialize camera position tracking
        lastCameraPosition = mainCamera.transform.position;
    }

    private void LateUpdate()
    {
        // Get current camera position
        Vector2 currentCameraPosition = mainCamera.transform.position;

        // Calculate camera movement delta
        Vector2 deltaMovement = currentCameraPosition - lastCameraPosition;

        // Update texture offset based on parallax speed (inverted for correct parallax effect)
        textureOffset += deltaMovement * parallaxSpeed;

        // Wrap offset values between 0-1 using modulo
        textureOffset.x = Mathf.Repeat(textureOffset.x, 1.0f);
        textureOffset.y = Mathf.Repeat(textureOffset.y, 1.0f);

        // Update shader parameters
        if (spriteRenderer.sprite.texture != material.GetTexture("_MainTex"))
        {
            material.SetTexture("_MainTex", spriteRenderer.sprite.texture);
        }

        material.SetFloat("_XOffset", textureOffset.x);
        material.SetFloat("_YOffset", textureOffset.y);

        // Update position to follow camera
        transform.position = new Vector2(currentCameraPosition.x, currentCameraPosition.y + yPosOffset);

        // Store current position for next frame
        lastCameraPosition = currentCameraPosition;

        if (debugMode) Debug.Log($"Texture offset: {textureOffset}");
    }
}
