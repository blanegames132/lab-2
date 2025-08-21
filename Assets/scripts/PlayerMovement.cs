using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private PlayerAnimatorController animatorController;

    [HideInInspector] public bool isGrounded = false; // Set externally

    // Internal state for movement
    private float hInput = 0f;
    private float vInput = 0f;

    void Awake()
    {
        if (!player) player = GetComponent<Rigidbody2D>();
        if (!animatorController) animatorController = GetComponent<PlayerAnimatorController>();
    }

    void Update()
    {
        // Get input in Update
        hInput = Input.GetAxisRaw("Horizontal");
        vInput = Input.GetAxisRaw("Vertical");

        // Jump (physics applied in FixedUpdate)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            player.linearVelocity = new Vector2(player.linearVelocity.x, jumpForce);
        }

        // Animation
        if (animatorController != null)
            animatorController.UpdateAnimator(hInput, false, isGrounded);
    }

    void FixedUpdate()
    {
        // X movement (split into forward and backward)
        float moveXF = 0f; // X Forward (right)
        float moveXB = 0f; // X Backward (left)

        if (hInput > 0)
            moveXF = hInput * speed * Time.fixedDeltaTime;
        if (hInput < 0)
            moveXB = hInput * speed * Time.fixedDeltaTime;

        // Move X forward (right) via Rigidbody2D
        if (moveXF > 0)
            player.MovePosition(player.position + new Vector2(moveXF, 0f));

        // Move X backward (left) via Rigidbody2D
        if (moveXB < 0)
            player.MovePosition(player.position + new Vector2(moveXB, 0f));

        // Z movement: move by +0.4 when pressing "W", -0.4 when pressing "S"
        if (Input.GetKeyDown(KeyCode.W))
            transform.position += new Vector3(0f, 0f, 0.1f);
        if (Input.GetKeyDown(KeyCode.S))
            transform.position += new Vector3(0f, 0f, -0.1f);
    }
}