using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Range(1f, 10f)] private float lifetime = 5f; // Lifetime of the projectile in seconds

    private Rigidbody2D rb;
    private bool shouldMatchParentDirection = false;
    private Transform firePointParent; // The parent (e.g., player) fire point

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Destroy the projectile after its lifetime expires
        Destroy(gameObject, lifetime);

        // Flip direction if needed at spawn
        if (shouldMatchParentDirection && firePointParent != null)
        {
            // For horizontal flip logic, assumes parent faces right when localScale.x > 0
            float parentFacing = Mathf.Sign(firePointParent.localScale.x);
            float myFacing = Mathf.Sign(transform.localScale.x);

            if (parentFacing != myFacing)
            {
                // Flip the projectile's X scale
                Vector3 scale = transform.localScale;
                scale.x *= -1f;
                transform.localScale = scale;
            }
        }
    }

    /// <summary>
    /// Call this immediately after instantiating the projectile if it is coming out of the parent's fire point,
    /// so it will flip to the direction the parent is facing.
    /// </summary>
    public void SetShouldMatchParentDirection(bool value, Transform parent)
    {
        shouldMatchParentDirection = value;
        firePointParent = parent;
    }

    // Set the projectile's velocity
    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
        else
        {
            Debug.LogError("Rigidbody2D component not found on the projectile.");
        }
    }
}