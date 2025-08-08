using UnityEngine;

public class BackgroundLayerControl : MonoBehaviour
{
    public SpriteRenderer playerSpriteRenderer; // Assign this in Inspector!
    private SpriteRenderer spriteRenderer;

    // Offset below player's layer (e.g., always 1 less)
    public int layerOffset = -1;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("Assign playerSpriteRenderer in the Inspector!");
        }
    }

    void Update()
    {
        if (playerSpriteRenderer != null)
        {
            int playerSortingOrder = playerSpriteRenderer.sortingOrder;
            int backgroundSortingOrder = playerSortingOrder + layerOffset;
            spriteRenderer.sortingOrder = backgroundSortingOrder;

            // Optionally sync Z position too, if you want:
            Vector3 pos = transform.position;
            pos.z = backgroundSortingOrder;
            transform.position = pos;
        }
    }
}