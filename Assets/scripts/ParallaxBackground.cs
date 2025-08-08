using UnityEngine;

/// <summary>
/// Controls parallax scrolling effect for background elements that follow the camera.
/// Keeps background Z position always 2 less than player's current Z position.
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
    private GameObject player;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        player = GameObject.FindGameObjectWithTag("Player");

        // Cache the material and set initial texture
        material = spriteRenderer.material;
        material.SetTexture("_MainTex", spriteRenderer.sprite.texture);

        lastCameraPosition = mainCamera.transform.position;
    }

    void LateUpdate()
    {
        // Parallax movement
        Vector2 currentCameraPosition = mainCamera.transform.position;
        Vector2 deltaMovement = currentCameraPosition - lastCameraPosition;
        textureOffset += deltaMovement * parallaxSpeed;

        if (spriteRenderer.sprite.texture != material.GetTexture("_MainTex"))
            material.SetTexture("_MainTex", spriteRenderer.sprite.texture);

        material.SetFloat("_XOffset", textureOffset.x);
        material.SetFloat("_YOffset", textureOffset.y);

        // Find player if missing
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        float playerZ = 0f;
        if (player != null)
        {
            playerZ = player.transform.position.z;
        }

        float bgZ = playerZ - 2;

        // Set background Z; can go infinitely positive or negative!
        transform.position = new Vector3(
            currentCameraPosition.x,
            currentCameraPosition.y + yPosOffset,
            bgZ
        );

        lastCameraPosition = currentCameraPosition;

        if (debugMode)
            Debug.Log($"Player Z: {playerZ}, Background Z: {bgZ}, Texture offset: {textureOffset}");
    }
}