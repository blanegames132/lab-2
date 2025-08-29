using UnityEngine;

/// <summary>
/// Controls enemy’s ability to see and attack the player based on view distance (X axis). 
/// Updates animator and enables following when in range.
/// </summary>
public class EnemyViewRange : MonoBehaviour
{
    public float viewDistance = 60f;
    public Transform target;
    public EnemyAnimatorController enemyAnimatorController; // Reference to animator controller
    public EnemyFollowPlayer followPlayer; // Reference to follow script

    [HideInInspector] public bool isAttacking = false;

    void Start()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (enemyAnimatorController == null)
            enemyAnimatorController = GetComponent<EnemyAnimatorController>();
        if (followPlayer == null)
            followPlayer = GetComponent<EnemyFollowPlayer>();
    }

    void Update()
    {
        if (target == null) return;

        float distToTarget = Mathf.Abs(transform.position.x - target.position.x);

        // Only attack/follow when the player is within viewDistance on X axis
        isAttacking = distToTarget <= viewDistance;

        // Update animator controller every frame
        if (enemyAnimatorController != null)
            enemyAnimatorController.SetAttacking(isAttacking);

        // Enable/disable following
        if (followPlayer != null)
            followPlayer.canFollow = isAttacking;
    }

    // Draw Gizmos in the editor for view distance visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 left = transform.position + Vector3.left * viewDistance;
        Vector3 right = transform.position + Vector3.right * viewDistance;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.3f);
        Gizmos.DrawWireSphere(right, 0.3f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}