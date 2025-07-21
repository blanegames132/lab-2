using UnityEngine;

public class PlayerControle : MonoBehaviour
{
    public float speedRight = 10f;
    public float speedLeft = 10f;
    public float jumpStrength = 3f;
    private Rigidbody2D rb;
    private isgrounded groundChecker;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        groundChecker = GetComponent<isgrounded>();
    }

    void Update()
    {
        float moveX = 0f;

        // A = Left
        if (Input.GetKey(KeyCode.A))
            moveX = -speedLeft;
        // D = Right
        if (Input.GetKey(KeyCode.D))
            moveX = speedRight;

        // Set horizontal velocity, keep gravity effect on y
        Vector2 velocity = rb.linearVelocity;
        velocity.x = moveX;
        rb.linearVelocity = velocity;

        // Jump only if grounded and W is pressed
        if (Input.GetKeyDown(KeyCode.W) && groundChecker.grounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpStrength);
        }
    }
}