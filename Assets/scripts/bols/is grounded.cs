using UnityEngine;
using UnityEngine.Tilemaps;

public class isgrounded : MonoBehaviour
{
    public bool grounded = false;

    private BoxCollider2D boxCollider;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        // Check if this BoxCollider2D is touching anything at all
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // Accept all layers and types

        Collider2D[] results = new Collider2D[5];
        int touching = boxCollider.Overlap(filter, results);

        grounded = touching > 0;
    }
}