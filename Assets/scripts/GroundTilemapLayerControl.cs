using UnityEngine;
using UnityEngine.Tilemaps;

public class GroundTilemapLayerControl : MonoBehaviour
{
    public SpriteRenderer playerSpriteRenderer; // Assign this in Inspector!
    private TilemapRenderer tilemapRenderer;

    // Offset below player's layer (e.g., always 1 less)
    public int layerOffset = -1;

    void Start()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        if (playerSpriteRenderer == null)
        {
            Debug.LogError("Assign playerSpriteRenderer in the Inspector!");
        }
        if (tilemapRenderer == null)
        {
            Debug.LogError("TilemapRenderer not found on this GameObject!");
        }
    }

    void Update()
    {
        if (playerSpriteRenderer != null && tilemapRenderer != null)
        {
            int playerSortingOrder = playerSpriteRenderer.sortingOrder;
            int groundSortingOrder = playerSortingOrder + layerOffset;
            tilemapRenderer.sortingOrder = groundSortingOrder;
        }
    }
}