using UnityEngine;

/// <summary>
/// Makes the enemy follow the player at a set speed. Attach to your enemy.
/// Set the player reference in the inspector or it will auto-find a GameObject tagged "Player".
/// </summary>
public class EnemyFollowPlayer : MonoBehaviour
{
    public Transform player; // Assign manually or auto-find by tag
    public float speedX = 0.2f;
    public float speedY = 0.5f;

    [HideInInspector] public bool canFollow = false;

    private void Awake()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (player == null || !canFollow) return;

        Vector3 target = player.position;
        Vector3 pos = transform.position;

        float stepX = speedX * Time.deltaTime;
        float stepY = speedY * Time.deltaTime;

        // Move towards player on X and Y independently, and match z exactly
        float newX = Mathf.MoveTowards(pos.x, target.x, stepX);
        float newY = Mathf.MoveTowards(pos.y, target.y, stepY);

        transform.position = new Vector3(newX, newY, target.z);
    }
}











