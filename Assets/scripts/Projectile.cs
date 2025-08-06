using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Range(1f, 10f)] private float lifetime = 5f; // Lifetime of the projectile in seconds

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        // Destroy the projectile after its lifetime expires
        Destroy(gameObject, lifetime);
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