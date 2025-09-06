using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Controls the rendering layer order (sortingOrder) of a SpriteRenderer or TilemapRenderer.
/// Attach to any GameObject with a SpriteRenderer or TilemapRenderer to control its sortingOrder.
/// </summary>
[DisallowMultipleComponent]
public class GenericLayerOrderController : MonoBehaviour
{
    [Tooltip("Set the desired sorting order for rendering (higher = drawn on top).")]
    public int layerOrder = 0;

    [Tooltip("Set the sorting layer name (optional, leave blank to ignore).")]
    public string sortingLayerName = "";

    private SpriteRenderer spriteRenderer;
    private TilemapRenderer tilemapRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        ApplyLayerOrder();
    }

    void OnValidate()
    {
        ApplyLayerOrder();
    }

    /// <summary>
    /// Applies the sorting order (and optionally sorting layer) to the renderer(s).
    /// </summary>
    public void ApplyLayerOrder()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = layerOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
                spriteRenderer.sortingLayerName = sortingLayerName;
        }
        if (tilemapRenderer != null)
        {
            tilemapRenderer.sortingOrder = layerOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
                tilemapRenderer.sortingLayerName = sortingLayerName;
        }
    }

    /// <summary>
    /// Change the layer order at runtime.
    /// </summary>
    public void SetLayerOrder(int newOrder, string newSortingLayer = "")
    {
        layerOrder = newOrder;
        if (!string.IsNullOrEmpty(newSortingLayer))
            sortingLayerName = newSortingLayer;
        ApplyLayerOrder();
    }
}