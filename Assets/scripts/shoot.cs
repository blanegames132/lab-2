using UnityEngine;

public class Shoot : MonoBehaviour
{
    private SpriteRenderer SR;

    [SerializeField] private Vector2 initialShotVelocity = new Vector2(5f, 0f); // Default bullet speed
    [SerializeField] private GameObject bullet = null; // Reference to the bullet prefab
    [SerializeField] private Transform firePoint = null; // Firing position

    void Start()
    {
        SR = GetComponent<SpriteRenderer>();

        if (initialShotVelocity == Vector2.zero)
            Debug.LogWarning("Initial shot velocity is set to zero. Assign a non-zero value for proper bullet movement.");
        if (firePoint == null)
            Debug.LogError("FirePoint Transform is not assigned in the inspector.");
        if (SR == null)
            Debug.LogError("SpriteRenderer component not found on the GameObject.");
        if (bullet == null)
            Debug.LogError("Bullet prefab is not assigned in the inspector.");
    }

    public void shoot()
    {
        if (bullet != null && firePoint != null)
        {
            GameObject newBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);

            // Always set the parent for correct effect
            Projectile proj = newBullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetFirePointParent(transform); // Pass spawner's transform
                proj.SetVelocity(initialShotVelocity);
            }
            else
            {
                Rigidbody2D rb = newBullet.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = initialShotVelocity;
                }
                else
                {
                    Debug.LogError("Bullet prefab has no Projectile script or Rigidbody2D component.");
                }
            }
        }
        else
        {
            Debug.LogError("Cannot shoot: Bullet prefab or FirePoint is not assigned.");
        }

        // Flip and fire logic (same as before)
        if (SR != null && SR.flipX)
        {
            initialShotVelocity.x = -Mathf.Abs(initialShotVelocity.x);
            firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            if (bullet != null)
            {
                SpriteRenderer bulletSR = bullet.GetComponent<SpriteRenderer>();
                if (bulletSR != null)
                {
                    bulletSR.flipX = true;
                }
            }
        }
        else
        {
            initialShotVelocity.x = Mathf.Abs(initialShotVelocity.x);
            firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            if (bullet != null)
            {
                SpriteRenderer bulletSR = bullet.GetComponent<SpriteRenderer>();
                if (bulletSR != null)
                {
                    bulletSR.flipX = false;
                }
            }
        }
    }
}