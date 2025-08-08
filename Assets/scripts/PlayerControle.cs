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
    private int sortingOrder = 0; // This is your z-layer

    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private float worldBottomY = -500f; // Matches your spawner

    private float layerChangeCooldown = 0.2f;
    private float fastLayerChangeCooldown = 0.05f;
    private float layerChangeTimer = 0f;

    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        groundChecker = GetComponent<isgrounded>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        sortingOrder = spriteRenderer.sortingOrder;

        SnapToGroundOnLayer(sortingOrder); // Start above ground
    }

    // Snap player to the nearest ground on the current layer (searches downward only)
    void SnapToGroundOnLayer(int layer)
    {
        Vector3 pos = transform.position;
        Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, layer));
        int maxSearchY = 100;
        for (int checkY = gridPos.y; checkY > gridPos.y - maxSearchY; checkY--)
        {
            Vector3Int checkPos = new Vector3Int(gridPos.x, checkY, layer);
            TileBase tile = groundTilemap.GetTile(checkPos);
            if (tile != null)
            {
                pos.y = checkY + 1.1f;
                pos.z = layer;
                transform.position = pos;
                Debug.Log($"Player snapped to surface at {pos}");
                return;
            }
        }
        // If no ground found, leave position as-is and warn
        Debug.LogWarning("No ground found under player on layer! Player may fall.");
    }

    // Checks if there is ground directly below the player for the current layer
    bool IsGroundedOnCurrentLayer()
    {
        Vector3 pos = transform.position;
        Vector3Int belowPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y - 1.1f, sortingOrder));
        TileBase tile = groundTilemap.GetTile(belowPos);
        return tile != null;
    }

    // Checks if there is ground at the player's current position on a target layer (for layer change)
    bool IsBlockedByGroundAtLayer(int layer)
    {
        Vector3 pos = transform.position;
        Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, layer));
        TileBase tile = groundTilemap.GetTile(gridPos);
        return tile != null;
    }

    // Checks if there is any ground on the target layer (for spawn/snap)
    bool HasGroundAtLayer(int layer)
    {
        Vector3 pos = transform.position;
        Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, layer));
        for (int checkY = gridPos.y; checkY > gridPos.y - 10; checkY--)
        {
            Vector3Int checkPos = new Vector3Int(gridPos.x, checkY, layer);
            TileBase tile = groundTilemap.GetTile(checkPos);
            if (tile != null) return true;
        }
        return false;
    }

    void Update()
    {
        float moveX = 0f;
        float hvalue = Input.GetAxis("Horizontal");
        layerChangeTimer -= Time.deltaTime;

        AnimatorStateInfo currrentstate = anim.GetCurrentAnimatorStateInfo(0);

        player.linearVelocity = new Vector2(hvalue * speed, player.linearVelocity.y);

        if (Input.GetMouseButtonDown(0))
            anim.SetBool("isattacking", true);
        else if (Input.GetMouseButtonUp(0))
            anim.SetBool("isattacking", false);

        if (IsGroundedOnCurrentLayer() && transform.eulerAngles.z != 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        if (Input.GetKey(KeyCode.A)) moveX = -speed;
        if (Input.GetKey(KeyCode.D)) moveX = speed;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        speed = isSprinting ? 20f : 10f;
        anim.SetBool("isrunning", isSprinting);

        if (hvalue < 0) spriteRenderer.flipX = true;
        else if (hvalue > 0) spriteRenderer.flipX = false;

        if (Input.GetKeyDown(KeyCode.Space) && IsGroundedOnCurrentLayer())
            player.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);

        float currentCooldown = isSprinting ? fastLayerChangeCooldown : layerChangeCooldown;
        if (layerChangeTimer <= 0f)
        {
            int previousOrder = sortingOrder;
            int newOrder = previousOrder;

            // Infinite Z movement: allow both up and down
            if (Input.GetKey(KeyCode.W))
                newOrder = sortingOrder - 1;
            if (Input.GetKey(KeyCode.S))
                newOrder = sortingOrder + 1;

            // Prevent layer change if ground is blocking the target layer at current position
            if (newOrder != previousOrder)
            {
                if (!IsBlockedByGroundAtLayer(newOrder) && HasGroundAtLayer(newOrder))
                {
                    sortingOrder = newOrder;
                    spriteRenderer.sortingOrder = sortingOrder;
                    SnapToGroundOnLayer(sortingOrder);
                    layerChangeTimer = currentCooldown;
                }
                else
                {
                    // Prevent change: ground in the way, or no ground at all
                    Debug.LogWarning($"Cannot change to layer {newOrder}: Blocked by ground or no ground at position!");
                }
            }
        }

        anim.SetFloat("hvalue", Mathf.Abs(hvalue));
        anim.SetBool("isGrounded", IsGroundedOnCurrentLayer());

        // FALL PROTECTION: Snap to ground if falling below world
        if (transform.position.y < worldBottomY)
        {
            Debug.LogWarning("Player fell below world! Respawning...");
            SnapToGroundOnLayer(sortingOrder);
            player.linearVelocity = Vector2.zero;
        }

        Debug.Log("isGrounded: " + IsGroundedOnCurrentLayer());
        Debug.Log("Player Position: " + player.position);
        Debug.Log("Player Velocity: " + player.linearVelocity);
    }
}