using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    public Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;

    void Awake()
    {
        if (!anim) anim = GetComponent<Animator>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void UpdateAnimator(float hValue, bool isRunning, bool isGrounded)
    {
        anim.SetBool("isrunning", isRunning);

        if (hValue < 0) spriteRenderer.flipX = true;
        else if (hValue > 0) spriteRenderer.flipX = false;

        anim.SetFloat("hvalue", Mathf.Abs(hValue));
        anim.SetBool("isGrounded", isGrounded);
    }

    public void SetAttacking(bool isAttacking)
    {
        anim.SetBool("isattacking", isAttacking);
    }
}