using UnityEngine;

public class PlayerControle : MonoBehaviour
{
    public float speed = 10f;
    
    private float jumpStrength = 10f;
    private Rigidbody2D player;
    private Animator anim;
    private isgrounded groundChecker;
    // ceate a variable for the animation controller
    //bool isrunning = false;


    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        groundChecker = GetComponent<isgrounded>();
    }

    void Update()
    {
        float moveX = 0f;
        float hvalue = Input.GetAxis("Horizontal");
        AnimatorStateInfo currrentstate = anim.GetCurrentAnimatorStateInfo(0);
        //Spriteflip(hvalue);
        player.linearVelocityX = hvalue * speed;

       



        // A = Left
        if (Input.GetKey(KeyCode.A))
            moveX = -speed;
        
        
        // D = Right
        if (Input.GetKey(KeyCode.D))
            moveX = speed;
        //if shif pressed , double the speed
        //speed= 20f;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 20f;
            anim.SetBool("isrunning", true);
        }
        //if shift is not pressed, set speed to 10f
        else
        {
            speed = 10f;
            anim.SetBool("isrunning", false);

        }

        // Set horizontal velocity, keep gravity effect on y
        // Vector2 velocity = player.linearVelocity;
        // velocity.x = moveX;
        //player.linearVelocity = velocity;

        // Jump only if grounded and W is pressed
        if (Input.GetKeyDown(KeyCode.W) && groundChecker.grounded)
        {
            // make jump with jumpStrength only if grounded
            player.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);

        }
        anim.SetFloat("hvalue", Mathf.Abs(hvalue));
        anim.SetBool("isGrounded", groundChecker.grounded);
        Debug.Log("isGrounded: " + groundChecker.grounded);
        //log player position for camera
        //Debug.Log("Player Position: " + player.position);
        //log player velocity for camera
        // Debug.Log("Player Velocity: " + player.linearVelocity);
        //log player grounded state for camera
        Debug.Log("Player Grounded: " + groundChecker.grounded);
    }

}