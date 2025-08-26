using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Range(1f, 10f)] private float lifetime = 5f;

    private Rigidbody2D rb;
    private Transform firePointParent; // This is what gets set

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);

        // Example: Flip direction to match parent
        if (firePointParent != null)
        {
            float parentFacing = Mathf.Sign(firePointParent.localScale.x);
            float myFacing = Mathf.Sign(transform.localScale.x);

            if (parentFacing != myFacing)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1f;
                transform.localScale = scale;
            }
        }
    }

    // <-- THIS IS THE METHOD YOU NEED!
    public void SetFirePointParent(Transform parent)
    {
        firePointParent = parent;
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
}