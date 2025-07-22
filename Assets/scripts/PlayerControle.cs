using UnityEngine;

public class PlayerControle : MonoBehaviour
{
    public float speedRight = 10f;
    public float speedLeft = 10f;
    private float jumpStrength = 10f;
    private Rigidbody2D player;
    private isgrounded groundChecker;

    void Start()
    {
        player = GetComponent<Rigidbody2D>();
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
        Vector2 velocity = player.linearVelocity;
        velocity.x = moveX;
        player.linearVelocity = velocity;

        // Jump only if grounded and W is pressed
        if (Input.GetKeyDown(KeyCode.W) && groundChecker.grounded)
        {
            // make jump with jumpStrength only if grounded
            player.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);
                
        }
        //log player position for camera
        //Debug.Log("Player Position: " + player.position);
        //log player velocity for camera
        // Debug.Log("Player Velocity: " + player.linearVelocity);
        //log player grounded state for camera
        Debug.Log("Player Grounded: " + groundChecker.grounded);
    }
}