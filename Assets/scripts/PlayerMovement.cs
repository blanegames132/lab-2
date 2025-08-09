using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles player movement, jump logic, and checks for walls to disable movement in blocked directions.
/// Prevents Z movement if a middle back tile or a debug ground tile is in the way.
/// Allows setting which tilemap should be used as the "middle back" tilemap.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float jumpSpeed = 12f;
    [SerializeField] private float maxJumpTime = 0.35f;

    [Header("References")]
    [SerializeField] private Rigidbody2D player;
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap; // You can now set this in the inspector!
    [SerializeField] private Tilemap debugGroundTilemap;
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private isgrounded groundChecker; // Your grounded script!

    [Header("Jump State")]
    [SerializeField] private bool isJumping = false;
    [SerializeField] private float jumpTimeCounter = 0f;

    void Awake()
    {
        if (!player) player = GetComponent<Rigidbody2D>();
        if (!animatorController) animatorController = GetComponent<PlayerAnimatorController>();
        if (!groundChecker) groundChecker = GetComponent<isgrounded>();
    }

    void Update()
    {
        bool isGrounded = groundChecker != null ? groundChecker.grounded : IsGrounded();

        // If grounded, set rotation back to zero
        if (isGrounded)
        {
            transform.rotation = Quaternion.identity;
        }

        // Set animator parameter
        if (animatorController != null && animatorController.anim != null)
        {
            animatorController.anim.SetBool("isGrounded", isGrounded);
        }

        // Movement XZ with wall checks
        float moveX = 0f;
        float moveZ = 0f;
        float hValue = Input.GetAxisRaw("Horizontal");
        float vValue = Input.GetAxisRaw("Vertical");

        if (hValue > 0 && !IsWall(Vector3.right))
            moveX = speed * Time.deltaTime;
        if (hValue < 0 && !IsWall(Vector3.left))
            moveX = -speed * Time.deltaTime;

        // Z movement is prevented if a middleBack tile or debug ground tile is in the way
        if (vValue > 0 && !IsWall(Vector3.forward) && !IsMiddleBackOrDebugInWay(Vector3.forward))
            moveZ = speed * Time.deltaTime;
        if (vValue < 0 && !IsWall(Vector3.back) && !IsMiddleBackOrDebugInWay(Vector3.back))
            moveZ = -speed * Time.deltaTime;

        transform.position += new Vector3(moveX, 0f, moveZ);

        // Jump Logic
        if (isGrounded && Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            isJumping = true;
            jumpTimeCounter = maxJumpTime;
            player.linearVelocity = new Vector2(player.linearVelocity.x, jumpSpeed);
        }
        if (isJumping && Input.GetKey(KeyCode.Space) && jumpTimeCounter > 0)
        {
            player.linearVelocity = new Vector2(player.linearVelocity.x, jumpSpeed);
            jumpTimeCounter -= Time.deltaTime;
        }
        if (Input.GetKeyUp(KeyCode.Space) || jumpTimeCounter <= 0)
        {
            isJumping = false;
        }
        if (isGrounded && !Input.GetKey(KeyCode.Space))
        {
            isJumping = false;
        }

        // Animator other logic
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        if (animatorController != null)
        {
            animatorController.UpdateAnimator(hValue, isRunning, isGrounded);

            if (Input.GetMouseButtonDown(0))
                animatorController.SetAttacking(true);
            else if (Input.GetMouseButtonUp(0))
                animatorController.SetAttacking(false);
        }
    }

    // Checks for any world tile (ground, middleFront, middleBack) as a wall
    bool IsWall(Vector3 direction)
    {
        Vector3 checkPos = transform.position + direction;
        int zLayer = Mathf.RoundToInt(checkPos.z);
        Vector3Int cell = groundTilemap.WorldToCell(new Vector3(checkPos.x, checkPos.y, zLayer));
        bool blocked = groundTilemap.GetTile(cell) != null ||
                       (middleBackTilemap != null && middleBackTilemap.GetTile(cell) != null) ||
                       (middleFrontTilemap != null && middleFrontTilemap.GetTile(cell) != null);
        return blocked;
    }

    // Prevent Z movement if middleBack or debug ground is in the way
    bool IsMiddleBackOrDebugInWay(Vector3 direction)
    {
        Vector3 checkPos = transform.position + direction;
        int zLayer = Mathf.RoundToInt(checkPos.z);
        Vector3Int cell = groundTilemap.WorldToCell(new Vector3(checkPos.x, checkPos.y, zLayer));
        bool blocked = false;

        if (middleBackTilemap != null && middleBackTilemap.GetTile(cell) != null)
            blocked = true;

        if (debugGroundTilemap != null && debugGroundTilemap.GetTile(cell) != null)
            blocked = true;

        return blocked;
    }

    bool IsGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f);
        return hit.collider != null;
    }
}