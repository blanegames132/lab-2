using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    [SerializeField] public Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Call this every frame from your movement/controller script.
    /// </summary>
    public void UpdateAnimator(float hValue, bool isRunning, bool isGrounded)
    {
        anim.SetBool("isrunning", isRunning);

        if (hValue < 0) spriteRenderer.flipX = true;
        else if (hValue > 0) spriteRenderer.flipX = false;

        anim.SetFloat("hvalue", Mathf.Abs(hValue));
        anim.SetBool("isGrounded", isGrounded);
    }

    /// <summary>
    /// Call when attacking state changes.
    /// </summary>
    public void SetAttacking(bool isAttacking)
    {
        anim.SetBool("isattacking", isAttacking);
    }
}