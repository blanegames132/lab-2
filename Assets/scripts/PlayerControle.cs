using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerControle : MonoBehaviour
{
    public float speed = 10f;
    private float jumpStrength = 10f;
    private Rigidbody2D player;
    private Animator anim;
    private isgrounded groundChecker;
    private SpriteRenderer spriteRenderer;
    private int sortingOrder = 0;

    // Add reference to the ground tilemap
    [SerializeField] private Tilemap groundTilemap;

    // For layer changing cooldown
    private float layerChangeCooldown = 0.2f; // Normal seconds between layer changes
    private float fastLayerChangeCooldown = 0.05f; // Faster cooldown when sprinting
    private float layerChangeTimer = 0f;

    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        groundChecker = GetComponent<isgrounded>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingOrder = spriteRenderer.sortingOrder;
    }

    void Update()
    {
        float moveX = 0f;
        float hvalue = Input.GetAxis("Horizontal");
        layerChangeTimer -= Time.deltaTime;

        AnimatorStateInfo currrentstate = anim.GetCurrentAnimatorStateInfo(0);

        // Set player horizontal velocity (fix for Rigidbody2D)
        player.linearVelocity = new Vector2(hvalue * speed, player.linearVelocity.y);

        if (Input.GetMouseButtonDown(0))
            anim.SetBool("isattacking", true);
        else if (Input.GetMouseButtonUp(0))
            anim.SetBool("isattacking", false);

        if (groundChecker.grounded && transform.eulerAngles.z != 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        // Move left/right with A/D
        if (Input.GetKey(KeyCode.A)) moveX = -speed;
        if (Input.GetKey(KeyCode.D)) moveX = speed;

        // Sprint with LeftShift
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        if (isSprinting)
        {
            speed = 20f;
            anim.SetBool("isrunning", true);
        }
        else
        {
            speed = 10f;
            anim.SetBool("isrunning", false);
        }

        // Sprite flip
        if (hvalue < 0) spriteRenderer.flipX = true;
        else if (hvalue > 0) spriteRenderer.flipX = false;

        // Jump with Space
        if (Input.GetKeyDown(KeyCode.Space) && groundChecker.grounded)
            player.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);

        // Layer changing with cooldown (faster with Shift)
        float currentCooldown = isSprinting ? fastLayerChangeCooldown : layerChangeCooldown;
        if (layerChangeTimer <= 0f)
        {
            // Move down a layer with W
            if (Input.GetKey(KeyCode.W))
            {
                sortingOrder -= 1;
                spriteRenderer.sortingOrder = sortingOrder;
                Vector3 pos = transform.position;
                pos.z = sortingOrder;

                // --- Check for ground block below in new Z layer ---
                Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, pos.z));
                for (int checkY = Mathf.FloorToInt(pos.y); checkY > Mathf.FloorToInt(pos.y) - 3; checkY--)
                {
                    Vector3Int belowPos = new Vector3Int(gridPos.x, checkY, sortingOrder);
                    TileBase tile = groundTilemap.GetTile(belowPos);
                    if (tile != null)
                    {
                        pos.y = checkY + 1.1f; // Move player up so its feet are above the block
                        break;
                    }
                }
                transform.position = pos;
                Debug.Log("Layer Down: sortingOrder=" + sortingOrder + ", z=" + pos.z);
                layerChangeTimer = currentCooldown;
            }
            // Move up a layer with S
            else if (Input.GetKey(KeyCode.S))
            {
                sortingOrder += 1;
                spriteRenderer.sortingOrder = sortingOrder;
                Vector3 pos = transform.position;
                pos.z = sortingOrder;

                // --- Check for ground block below in new Z layer ---
                Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, pos.z));
                for (int checkY = Mathf.FloorToInt(pos.y); checkY > Mathf.FloorToInt(pos.y) - 3; checkY--)
                {
                    Vector3Int belowPos = new Vector3Int(gridPos.x, checkY, sortingOrder);
                    TileBase tile = groundTilemap.GetTile(belowPos);
                    if (tile != null)
                    {
                        pos.y = checkY + 1.1f; // Move player up so its feet are above the block
                        break;
                    }
                }
                transform.position = pos;
                Debug.Log("Layer Up: sortingOrder=" + sortingOrder + ", z=" + pos.z);
                layerChangeTimer = currentCooldown;
            }
        }

        anim.SetFloat("hvalue", Mathf.Abs(hvalue));
        anim.SetBool("isGrounded", groundChecker.grounded);

        Debug.Log("isGrounded: " + groundChecker.grounded);
        Debug.Log("Player Position: " + player.position);
        Debug.Log("Player Velocity: " + player.linearVelocity);
        Debug.Log("Player Grounded: " + groundChecker.grounded);
    }
}