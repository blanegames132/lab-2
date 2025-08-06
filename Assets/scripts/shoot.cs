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
        {
            Debug.LogWarning("Initial shot velocity is set to zero. Assign a non-zero value for proper bullet movement.");
        }

        if (firePoint == null)
        {
            Debug.LogError("FirePoint Transform is not assigned in the inspector.");
        }

        if (SR == null)
        {
            Debug.LogError("SpriteRenderer component not found on the GameObject.");
        }

        if (bullet == null)
        {
            Debug.LogError("Bullet prefab is not assigned in the inspector.");
        }
    }

    public void shoot()
    {
        // Only shoot if bullet prefab and fire point are assigned
        if (bullet != null && firePoint != null)
        {
            // Instantiate the bullet at the fire point
            GameObject newBullet = Instantiate(bullet, firePoint.position, firePoint.rotation);

            // Check for Projectile script and set its velocity
            Projectile proj = newBullet.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.SetVelocity(initialShotVelocity);
            }
            else
            {
                // Fallback: If no Projectile script, try Rigidbody2D directly
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
        //if hvalue < 0, flip the initial shot velocity to left
        if (SR != null && SR.flipX)
        {
            initialShotVelocity.x = -Mathf.Abs(initialShotVelocity.x);
            //move fire point to left side of player
            firePoint.localPosition = new Vector3(-Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            //filip bullet sprite to left if hvalue < 0
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
            // retern fire point to right side of player
            firePoint.localPosition = new Vector3(Mathf.Abs(firePoint.localPosition.x), firePoint.localPosition.y, firePoint.localPosition.z);
            //filip bullet sprite to right if hvalue > 0
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