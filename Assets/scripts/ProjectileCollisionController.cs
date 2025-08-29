using UnityEngine;

/// <summary>
/// Attach to any GameObject (such as a bullet).
/// On collision with ANY collider:
///   - Deals damageAmount to EnemyHealth if present
///   - Always logs collision/damage
///   - Always destroys itself
/// </summary>
public class DealDamageAndAlwaysDestroyOnCollision : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt if colliding object has EnemyHealth")]
    public int damageAmount = 3;

    private void OnCollisionEnter(Collision collision)
    {
        // Deal damage if possible
        EnemyHealth enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageAmount);
            Debug.Log($"{gameObject.name} dealt {damageAmount} damage to {collision.gameObject.name}");
        }
        else
        {
            Debug.Log($"{gameObject.name} collided with {collision.gameObject.name} (no EnemyHealth found)");
        }

        // Always destroy self on any collision
        Destroy(gameObject);
    }
}