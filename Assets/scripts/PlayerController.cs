using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public PlayerAnimatorController animController;

    void Update()
    {
        // On mouse click down, set isAttacking to true
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Mouse click DOWN! Setting isAttacking true.");
            animController.SetAttacking(true);
        }

        // Optional: Set isAttacking to false when releasing the mouse button
        if (Input.GetMouseButtonUp(1))
        {
            Debug.Log("Mouse click UP! Setting isAttacking false.");
            animController.SetAttacking(false);
        }

        // Example movement logic (replace with your real logic if needed)
        /*
        float hValue = Input.GetAxisRaw("Horizontal");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        bool isGrounded = true; // Replace with actual ground check

        animController.UpdateAnimator(hValue, isRunning, isGrounded);
        */
    }
}