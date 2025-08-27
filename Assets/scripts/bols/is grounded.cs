using UnityEngine;

public class isgrounded : MonoBehaviour
{
    public bool grounded = false;

    [SerializeField] private PlayerLeftRightMovement playerMovement; // Assign in inspector

    private Collider2D myCollider;

    void Start()
    {
        myCollider = GetComponent<Collider2D>();
        if (myCollider == null)
        {
            Debug.LogError("No 2D Collider found on this GameObject.");
        }
    }

    void Update()
    {
        if (myCollider == null)
        {
            grounded = false;
            return;
        }

        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // Accept all layers and types

        Collider2D[] results = new Collider2D[5];
        int touching = myCollider.Overlap(filter, results);

        // Exclude self-collision
        int actualTouching = 0;
        for (int i = 0; i < touching; i++)
        {
            if (results[i] != myCollider && results[i] != null)
            {
                actualTouching++;
            }
        }

        grounded = actualTouching > 0;

        // Update PlayerLeftRightMovement's isGrounded variable if assigned
        if (playerMovement != null)
        {
            playerMovement.isGrounded = grounded;
        }
    }
}