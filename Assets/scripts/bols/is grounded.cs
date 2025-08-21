using UnityEngine;

public class isgrounded : MonoBehaviour
{
    public bool grounded = false;

    [SerializeField] private PlayerMovement playerMovement; // Assign in inspector

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

        // Update PlayerMovement's isGrounded variable if assigned
        if (playerMovement != null)
        {
            playerMovement.isGrounded = grounded;
        }
    }
}