using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator anim; // Assign in Inspector or auto-assign
    [SerializeField] private SpriteRenderer spriteRenderer; // Assign in Inspector or auto-assign

    void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Set the attacking state in the animator ("isAttacking" parameter, capital A).
    /// </summary>
    public void SetAttacking(bool isAttacking)
    {
        anim.SetBool("isAttacking", isAttacking);
    }
}