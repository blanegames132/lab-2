using UnityEngine;

public class EnemyViewRange : MonoBehaviour
{
    public float viewDistance = 10f;
    public Transform target;
    public EnemyAnimatorController enemyAnimatorController; // Reference to animator controller

    [HideInInspector] public bool isAttacking = false;

    void Start()
    {
        if (target == null)
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!enemyAnimatorController)
            enemyAnimatorController = GetComponent<EnemyAnimatorController>();
    }

    void Update()
    {
        isAttacking = false;
        if (target == null) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);

        if (distToTarget <= viewDistance)
        {
            isAttacking = true;
        }

        // Pass isAttacking to the animator controller every frame
        if (enemyAnimatorController != null)
        {
            enemyAnimatorController.SetAttacking(isAttacking);
        }
    }
}