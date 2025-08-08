using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PlayerControle : MonoBehaviour
{
    public float speed = 10f;
    private float jumpStrength = 10f;
    private Rigidbody2D player;
    private Animator anim;
    private isgrounded groundChecker;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private float worldBottomY = -500f;

    private TileInfiniteCameraSpawner spawner;

    private bool movementLocked = false;
    private float movementLockTimer = 0f;
    private const float movementLockDuration = 0.05f;

    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        groundChecker = GetComponent<isgrounded>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawner = FindObjectOfType<TileInfiniteCameraSpawner>();

        ClampToNearestGroundAtStart();
        UpdateSortingOrder();
    }

    void ClampToNearestGroundAtStart()
    {
        if (spawner == null)
        {
            Debug.LogWarning("No TileInfiniteCameraSpawner found!");
            return;
        }
        Vector3 pos = transform.position;
        int x = Mathf.RoundToInt(pos.x);
        int z = Mathf.RoundToInt(pos.z);

        int surfaceY = spawner.GetSurfaceY(x, z);

        pos.y = surfaceY + 1.1f;
        pos.z = z;
        transform.position = pos;
        UpdateSortingOrder();
        Debug.Log($"Player clamped to ground at {pos}");
    }

    public void SnapToGroundOnLayer(int zLayer)
    {
        Vector3 pos = transform.position;
        Vector3Int gridPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y, zLayer));
        int maxSearchY = 100;
        for (int checkY = gridPos.y; checkY > gridPos.y - maxSearchY; checkY--)
        {
            Vector3Int checkPos = new Vector3Int(gridPos.x, checkY, zLayer);
            if (IsBlockedTile(checkPos)) continue;
            TileBase tile = GetAnyGroundTile(checkPos);
            if (tile != null)
            {
                pos.y = checkY + 1.1f;
                pos.z = zLayer;
                transform.position = pos;
                UpdateSortingOrder();
                Debug.Log($"Player snapped to surface at {pos}");
                return;
            }
        }
        Debug.LogWarning("No ground found under player on layer! Player may fall.");
    }

    TileBase GetAnyGroundTile(Vector3Int pos)
    {
        TileBase g = groundTilemap.GetTile(pos);
        if (g != null) return g;
        TileBase mf = middleFrontTilemap.GetTile(pos);
        if (mf != null) return mf;
        TileBase mb = middleBackTilemap.GetTile(pos);
        if (mb != null) return mb;
        return null;
    }

    bool IsBlockedTile(Vector3Int cell)
    {
        return groundTilemap.GetTile(cell) != null ||
               middleBackTilemap.GetTile(cell) != null ||
               middleFrontTilemap.GetTile(cell) != null;
    }

    bool IsPositionBlocked(Vector3 position)
    {
        int zLayer = Mathf.RoundToInt(position.z);
        Vector3Int cell = groundTilemap.WorldToCell(new Vector3(position.x, position.y, zLayer));
        return IsBlockedTile(cell);
    }

    // --- MODIFIED FUNCTION ---
    // Finds the closest free spot to a target position, within a max radius
    Vector3? FindClosestFreeSpot(Vector3 targetPosition, int maxRadius = 8)
    {
        int targetZ = Mathf.RoundToInt(targetPosition.z);
        Vector3Int targetCell = groundTilemap.WorldToCell(targetPosition);

        Vector3? closestPos = null;
        float closestDist = float.MaxValue;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius && Mathf.Abs(dz) != radius) continue;
                        int checkZ = targetZ + dz;
                        Vector3Int checkCell = new Vector3Int(targetCell.x + dx, targetCell.y + dy, checkZ);

                        if (IsBlockedTile(checkCell)) continue;

                        Vector3 freePos = groundTilemap.CellToWorld(checkCell) + new Vector3(0.5f, 0.5f, 0f);
                        float dist = Vector3.Distance(targetPosition, new Vector3(freePos.x, freePos.y, checkZ));
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestPos = new Vector3(freePos.x, freePos.y, checkZ);
                        }
                    }
                }
            }
            if (closestPos != null) break; // break early if found in smaller radius
        }
        return closestPos;
    }

    // Push player to the closest free spot, considering proximity to intended move
    void PushToClosestOpenArea(Vector3? intendedTarget = null)
    {
        Vector3 pos = transform.position;
        Vector3 target = intendedTarget ?? pos;

        Vector3? closestFree = FindClosestFreeSpot(target);

        if (closestFree != null)
        {
            transform.position = closestFree.Value;
            player.linearVelocity = Vector2.zero;
            UpdateSortingOrder();
            Debug.Log("Player pushed to closest open area: " + closestFree);

            movementLocked = true;
            movementLockTimer = movementLockDuration;
        }
        else
        {
            Debug.LogWarning("No open area found nearby!");
        }
    }

    bool IsGroundedOnCurrentLayer()
    {
        Vector3 pos = transform.position;
        int zLayer = Mathf.RoundToInt(pos.z);
        Vector3Int belowPos = groundTilemap.WorldToCell(new Vector3(pos.x, pos.y - 1.1f, zLayer));
        return GetAnyGroundTile(belowPos) != null;
    }

    void UpdateSortingOrder()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(transform.position.z);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && IsGroundedOnCurrentLayer())
        {
            player.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);
        }

        if (movementLocked)
        {
            movementLockTimer -= Time.deltaTime;
            if (movementLockTimer <= 0f)
            {
                movementLocked = false;
            }
            else
            {
                player.linearVelocity = Vector2.zero;
                return;
            }
        }

        float hValue = Input.GetAxisRaw("Horizontal");
        float vValue = Input.GetAxisRaw("Vertical");
        float moveX = hValue * speed;
        float moveZ = vValue * speed;

        // --- MODIFIED COLLISION-AWARE MOVEMENT LOGIC ---
        Vector3 prospectiveMove = transform.position + new Vector3(moveX * Time.deltaTime, 0f, moveZ * Time.deltaTime);

        if (IsPositionBlocked(transform.position))
        {
            Debug.Log("Player is overlapping blocked tile, movement disabled and will be pushed.");
            PushToClosestOpenArea(prospectiveMove); // pass intended move
            player.linearVelocity = Vector2.zero;
            return;
        }

        // Always check for a closer free spot
        if (!IsPositionBlocked(prospectiveMove))
        {
            Vector3? closerFree = FindClosestFreeSpot(prospectiveMove);
            if (closerFree != null && Vector3.Distance(transform.position, closerFree.Value) < Vector3.Distance(transform.position, prospectiveMove))
            {
                transform.position = closerFree.Value;
            }
            else
            {
                transform.position = prospectiveMove;
            }
            UpdateSortingOrder();
        }

        speed = Input.GetKey(KeyCode.LeftShift) ? 20f : 10f;
        anim.SetBool("isrunning", Input.GetKey(KeyCode.LeftShift));

        if (hValue < 0) spriteRenderer.flipX = true;
        else if (hValue > 0) spriteRenderer.flipX = false;

        anim.SetFloat("hvalue", Mathf.Abs(hValue));
        anim.SetBool("isGrounded", IsGroundedOnCurrentLayer());

        if (Input.GetMouseButtonDown(0))
            anim.SetBool("isattacking", true);
        else if (Input.GetMouseButtonUp(0))
            anim.SetBool("isattacking", false);

        if (transform.position.y < worldBottomY)
        {
            Debug.LogWarning("Player fell below world! Respawning...");
            SnapToGroundOnLayer(Mathf.RoundToInt(transform.position.z));
            player.linearVelocity = Vector2.zero;
        }

        Debug.Log("isGrounded: " + IsGroundedOnCurrentLayer());
        Debug.Log("Player Position: " + player.position);
        Debug.Log("Player Velocity: " + player.linearVelocity);
    }
}