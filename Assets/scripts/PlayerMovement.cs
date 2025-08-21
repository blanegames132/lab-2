using UnityEngine;

public class PlayerLeftRightMovement : MonoBehaviour
{
    [SerializeField] private float leftSpeed = 10f;
    [SerializeField] private float rightSpeed = 10f;
    [SerializeField] private float leftRunSpeed = 16f;
    [SerializeField] private float rightRunSpeed = 16f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private PlayerAnimatorController animatorController;

    [HideInInspector] public bool isGrounded = false; // Set externally by a ground check script

    private float hInput = 0f;
    private bool isRunning = false;

    void Awake()
    {
        if (!player) player = GetComponent<Rigidbody2D>();
        if (!animatorController) animatorController = GetComponent<PlayerAnimatorController>();
    }

    void Update()
    {
        // Set hInput based on A/D keys
        if (Input.GetKey(KeyCode.A))
            hInput = -1f;
        else if (Input.GetKey(KeyCode.D))
            hInput = 1f;
        else
            hInput = 0f;

        // Check for running (Shift held)
        isRunning = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && hInput != 0f;

        // Jump if grounded and space is pressed
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            player.linearVelocity = new Vector2(player.linearVelocity.x, jumpForce);
        }

        // Animation and flip
        if (animatorController != null)
            animatorController.UpdateAnimator(hInput, isRunning, isGrounded);
    }

    void FixedUpdate()
    {
        float speed;
        if (isRunning)
            speed = hInput < 0 ? leftRunSpeed : (hInput > 0 ? rightRunSpeed : 0f);
        else
            speed = hInput < 0 ? leftSpeed : (hInput > 0 ? rightSpeed : 0f);

        player.linearVelocity = new Vector2(hInput * speed, player.linearVelocity.y);
    }
}