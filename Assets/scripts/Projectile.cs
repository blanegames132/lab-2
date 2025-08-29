using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Range(1f, 10f)] private float lifetime = 5f;
    [SerializeField] private int damageAmount = 3;

    private Rigidbody2D rb;
    private Transform firePointParent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);

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

    // Damage enemy and always destroy on collision (for non-trigger colliders)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealDamage(collision.gameObject);
        Destroy(gameObject);
    }

    // Damage enemy and always destroy on trigger (for trigger colliders)
    private void OnTriggerEnter2D(Collider2D collider)
    {
        TryDealDamage(collider.gameObject);
        Destroy(gameObject);
    }

    private void TryDealDamage(GameObject other)
    {
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
            Debug.Log($"{gameObject.name} dealt {damageAmount} damage to {other.name}");
        }
    }
}