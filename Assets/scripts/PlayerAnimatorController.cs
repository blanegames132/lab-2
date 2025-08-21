using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    public Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float runSpeed = 16f; // Set in Inspector

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Call this from your movement script.
    /// hValue: -1, 0, or 1 for walk only (not running)
    /// isRunning: true if running (shift held), false if walking
    /// isGrounded: true if on ground
    /// </summary>
    public void UpdateAnimator(float hValue, bool isRunning, bool isGrounded)
    {
        anim.SetBool("isrunning", isRunning);

        // Flip sprite for direction
        if (hValue < 0) spriteRenderer.flipX = true;
        else if (hValue > 0) spriteRenderer.flipX = false;

        // hvalue is only for walk animation
        anim.SetFloat("hvalue", Mathf.Abs(hValue));
        anim.SetBool("isGrounded", isGrounded);
    }

    public void SetAttacking(bool isAttacking)
    {
        anim.SetBool("isattacking", isAttacking);
    }

    /// <summary>
    /// Returns the run speed for use in your movement script.
    /// </summary>
    public float GetRunSpeed()
    {
        return runSpeed;
    }
}