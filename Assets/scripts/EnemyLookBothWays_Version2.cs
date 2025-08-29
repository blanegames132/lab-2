using UnityEngine;

/// <summary>
/// Idle turret: flips at random intervals, "sees" player with a view cone,
/// sets isAttacking (and Animator "isAttacking" bool) when player is in cone,
/// stops flipping while attacking, supports separate attack range.
/// </summary>
public class EnemyIdleTurret : MonoBehaviour
{
    public Transform player; // Assign manually or auto-find by tag
    public float speedX = 0.2f; // Unused, for compatibility
    public float speedY = 0.5f; // Unused, for compatibility

    [HideInInspector] public bool canFollow = false; // Unused, for compatibility

    [Header("Idle Turret Look Settings")]
    public float minLookInterval = 1.5f;
    public float maxLookInterval = 3.5f;

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer; // Assign manually or auto-find

    [Header("View Cone Settings")]
    public float viewConeLength = 5f;
    public float viewConeAngle = 45f; // Half angle (total cone = 90deg)

    [Header("Attack Range Settings")]
    public float attackRange = 3f; // Separate range check for attack logic

    [Header("Animator")]
    public Animator animator; // Assign or auto-find

    [Header("Attack State (Read Only)")]
    public bool isAttacking = false;
    public bool inAttackRange = false;

    private float lookTimer = 0f;
    private float nextLookTime = 0f;
    private bool isFlipped = false;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        SetNextLookTime();
    }

    void Update()
    {
        // Check attack and view cone states
        isAttacking = PlayerInViewCone();
        inAttackRange = PlayerInAttackRange();

        // Set animator parameter
        if (animator != null)
            animator.SetBool("isAttacking", isAttacking);

        // Only flip if NOT attacking (player is not in view cone)
        if (!isAttacking)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer >= nextLookTime)
            {
                FlipLookDirection();
                SetNextLookTime();
            }
        }
        // else: do not flip, stay facing current direction
    }

    void SetNextLookTime()
    {
        lookTimer = 0f;
        nextLookTime = Random.Range(minLookInterval, maxLookInterval);
    }

    void FlipLookDirection()
    {
        isFlipped = !isFlipped;
        if (spriteRenderer != null)
            spriteRenderer.flipX = isFlipped;
    }

    // Returns true if player is in the current view cone
    bool PlayerInViewCone()
    {
        if (player == null) return false;

        Vector3 facing = (spriteRenderer != null && spriteRenderer.flipX) ? Vector3.left : Vector3.right;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.z = 0f; // 2D: use XY; change to y=0 if game is XZ

        if (toPlayer.magnitude > viewConeLength)
            return false;

        float angle = Vector3.Angle(facing, toPlayer);
        return angle <= viewConeAngle;
    }

    // Returns true if player is within attack range (separate from view cone)
    bool PlayerInAttackRange()
    {
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.z = 0f;
        return toPlayer.magnitude <= attackRange;
    }

    // Draw view cone and attack range in Scene View
    void OnDrawGizmosSelected()
    {
        if (spriteRenderer == null && Application.isPlaying == false)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Vector3 facing = (spriteRenderer != null && spriteRenderer.flipX) ? Vector3.left : Vector3.right;
        Vector3 origin = transform.position;
        float halfAngle = viewConeAngle;

        // Cone boundaries
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.forward);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.forward);
        Vector3 leftDir = leftRot * facing;
        Vector3 rightDir = rightRot * facing;

        Gizmos.color = new Color(1f, 1f, 0.3f, 0.3f);
        Gizmos.DrawLine(origin, origin + leftDir * viewConeLength);
        Gizmos.DrawLine(origin, origin + rightDir * viewConeLength);

        // Fill cone (optional)
        int segments = 16;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Quaternion rot = Quaternion.AngleAxis(angle, Vector3.forward);
            Vector3 dir = rot * facing;
            Gizmos.DrawLine(origin, origin + dir * viewConeLength);
        }

        // Draw attack range circle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);
    }
}