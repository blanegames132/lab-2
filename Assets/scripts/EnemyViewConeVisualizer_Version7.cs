using UnityEngine;

/// <summary>
/// Controls the enemy Animator and sprite facing direction.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator anim; // Assign in Inspector or auto-assign
    [SerializeField] private SpriteRenderer spriteRenderer; // Assign in Inspector or auto-assign
    [SerializeField] private Transform player; // Optional, for sprite flip

    void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    /// <summary>
    /// Set the attacking state in the animator ("isAttacking" parameter) and face the player.
    /// </summary>
    public void SetAttacking(bool isAttacking)
    {
        if (anim != null)
            anim.SetBool("isAttacking", isAttacking);

        // Optional: flip sprite to look at player
        if (spriteRenderer != null && player != null)
            spriteRenderer.flipX = (player.position.x < transform.position.x);
    }
}