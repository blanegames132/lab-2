using UnityEngine;

/// <summary>
/// Enemy stays in place, flips left/right at intervals, sets isAttacking true
/// only when player is in front (2.5D: X/Z axes, ignores Y).
/// Stops flipping and sets animator isAttacking true when attacking.
/// </summary>
public class EnemyIdleLookAndAttack : MonoBehaviour
{
    [Header("Look Timing")]
    public float minLookInterval = 1.5f;
    public float maxLookInterval = 3.5f;

    [Header("Optional: assign the visual child to flip (if mesh/sprite is not on root)")]
    public Transform visualChild;

    [Header("Detection")]
    public float detectionRange = 4f;
    public float fieldOfView = 90f; // in degrees, for detection cone
    public LayerMask playerLayer;

    [Header("Animator")]
    public Animator animator; // Assign in Inspector or auto-find

    [Header("Player Reference")]
    public Transform player; // Show in Inspector

    [Header("Debug")]
    public bool isFlipped = false;    // True if facing left, false if facing right
    public bool isAttacking = false;  // True if player in detection area

    private float lookTimer = 0f;
    private float nextLookTime = 0f;
    private bool hasDetectedPlayer = false; // To limit Debug.Log to once per detection

    private void Awake()
    {
        // Auto-find player if not assigned
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // Auto-find Animator if not assigned
        if (animator == null) animator = GetComponent<Animator>();

        SetNextLookTime();
    }

    void Update()
    {
        // Detect player in front (updates isAttacking)
        DetectPlayer();

        // Set animator parameter for transitions
        if (animator != null)
            animator.SetBool("isAttacking", isAttacking); // lowercase "i" to match your Animator

        // Debug log if detected player (only log once per detection cycle)
        if (isAttacking && !hasDetectedPlayer)
        {
            Debug.Log($"{gameObject.name} (Zombie) detected player!");
            hasDetectedPlayer = true;
        }
        if (!isAttacking)
        {
            hasDetectedPlayer = false;
        }

        // Only flip when NOT attacking
        if (!isAttacking)
        {
            lookTimer += Time.deltaTime;
            if (lookTimer >= nextLookTime)
            {
                FlipLookDirection();
                SetNextLookTime();
            }
        }
    }

    void SetNextLookTime()
    {
        lookTimer = 0f;
        nextLookTime = Random.Range(minLookInterval, maxLookInterval);
    }

    void FlipLookDirection()
    {
        isFlipped = !isFlipped;
        if (visualChild)
        {
            Vector3 scale = visualChild.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFlipped ? -1f : 1f);
            visualChild.localScale = scale;
        }
        else
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (isFlipped ? -1f : 1f);
            transform.localScale = scale;
        }
    }

    void DetectPlayer()
    {
        if (player == null)
        {
            isAttacking = false;
            return;
        }

        // 2.5D: Ignore Y axis for detection
        Vector3 dirToPlayer = player.position - transform.position;
        dirToPlayer.y = 0f; // Only X and Z are considered

        float distance = dirToPlayer.magnitude;
        if (distance > detectionRange)
        {
            isAttacking = false;
            return;
        }

        Vector3 facingDir = isFlipped ? Vector3.left : Vector3.right;

        // Field of view angle check (in X/Z plane)
        float dot = Vector3.Dot(dirToPlayer.normalized, facingDir);
        float angleToPlayer = Mathf.Acos(dot) * Mathf.Rad2Deg;

        isAttacking = angleToPlayer <= (fieldOfView * 0.5f);
    }

    void OnDrawGizmos()
    {
        // Draw look direction
        Gizmos.color = isAttacking ? Color.yellow : Color.red;
        Vector3 start = visualChild ? visualChild.position : transform.position;
        Vector3 facingDir = isFlipped ? Vector3.left : Vector3.right;
        Gizmos.DrawLine(start, start + facingDir * 2f);

        // Draw detection range
        Gizmos.color = new Color(1f, 1f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(start, detectionRange);

        // Draw detection cone (X/Z plane)
        Gizmos.color = new Color(1f, 1f, 0.1f, 0.13f);
        float halfAngle = fieldOfView * 0.5f;
        Quaternion leftRot = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftDir = leftRot * facingDir;
        Vector3 rightDir = rightRot * facingDir;
        Gizmos.DrawLine(start, start + leftDir * detectionRange);
        Gizmos.DrawLine(start, start + rightDir * detectionRange);
    }
}