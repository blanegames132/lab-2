using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles player control including movement lock, sorting, ground snap, and directional collision blocking.
/// If player overlaps with middle ground on Z, pushes back to the previous layer (the layer you came from).
/// The push is only on Z, so it does not act like a jump (Y unaffected).
/// If a tile is hidden in front of the player, prevents -Z movement.
/// When moving Z and grounded, sticks to ground.
/// If stuck in ground, pushes player to the closest empty collision spot.
/// If player cannot move (all directions blocked), prints "Player is stuck" debug message.
/// </summary>
public class PlayerControle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private isgrounded groundChecker;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TileInfiniteCameraSpawner spawner;
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Tiles & World")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private float worldBottomY = -500f;

    [Header("Movement Lock")]
    [SerializeField] private bool movementLocked = false;
    [SerializeField] private float movementLockTimer = 0f;
    [SerializeField] private float movementLockDuration = 0.05f;

    [Header("Movement")]
    [SerializeField] private float speed = 10f;

    private BoxCollider2D boxCollider;
    private int previousZLayer;

    void Start()
    {
        if (!groundChecker) groundChecker = GetComponent<isgrounded>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        if (!animatorController) animatorController = GetComponent<PlayerAnimatorController>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();
        boxCollider = GetComponent<BoxCollider2D>();

        previousZLayer = Mathf.RoundToInt(transform.position.z);
        UpdateSortingOrder();
    }

    void Update()
    {
        if (movementLocked)
        {
            movementLockTimer -= Time.deltaTime;
            if (movementLockTimer <= 0f)
            {
                movementLocked = false;
            }
            return;
        }

        int currentZLayer = Mathf.RoundToInt(transform.position.z);
        Vector3Int cellPos = groundTilemap.WorldToCell(transform.position);

        // Check overlap with middle ground (front or back) on Z
        if (IsOverlappingMiddleGround(cellPos))
        {
            Vector3 pos = transform.position;
            pos.z = previousZLayer;
            transform.position = pos;
            UpdateSortingOrder();
            movementLocked = true;
            movementLockTimer = movementLockDuration;
            Debug.Log($"Player pushed back to previous layer Z={previousZLayer} due to overlap with middle ground.");
            return;
        }

        // Check for hidden tile at exact player X/Y (any Z in the layers)
        if (spawner != null)
        {
            Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            int[] layerZs = new int[] {
                Mathf.RoundToInt(transform.position.z) - 2,
                Mathf.RoundToInt(transform.position.z) - 1,
                Mathf.RoundToInt(transform.position.z),
                Mathf.RoundToInt(transform.position.z) + 1,
                Mathf.RoundToInt(transform.position.z) + 2
            };
            foreach (int z in layerZs)
            {
                Vector3Int checkCell = new Vector3Int(playerXY.x, playerXY.y, z);
                if (spawner.IsTileHidden(checkCell))
                {
                    Debug.Log($"Tile hidden under player at ({playerXY.x}, {playerXY.y}, {z})");
                    break;
                }
            }
        }

        // Prevent -Z movement if hidden tile is in front of the player
        bool hiddenInFront = false;
        if (spawner != null)
        {
            Vector2Int playerXY = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            int frontZ = Mathf.RoundToInt(transform.position.z) - 1;
            Vector3Int frontCell = new Vector3Int(playerXY.x, playerXY.y, frontZ);
            if (spawner.IsTileHidden(frontCell))
            {
                hiddenInFront = true;
                Debug.Log("Tile hidden in front of player (-Z), -Z movement blocked.");
            }
        }

        float moveX = 0f;
        float moveY = 0f;
        float moveZ = 0f;

        bool canMoveRight = !IsTouching(Vector2.right);
        bool canMoveLeft = !IsTouching(Vector2.left);
        bool canMoveUp = !IsTouching(Vector2.up);
        bool canMoveDown = !IsTouching(Vector2.down);

        // Z movement keys
        bool canMoveZMinus = !hiddenInFront;
        bool canMoveZPlus = true; // You can add logic for +Z if needed

        // If all directions are blocked (no movement possible), print stuck debug
        if (!canMoveRight && !canMoveLeft && !canMoveUp && !canMoveDown && !canMoveZMinus && !canMoveZPlus)
        {
            Debug.Log("Player is stuck");
        }

        if (Input.GetAxisRaw("Horizontal") > 0 && canMoveRight)
            moveX = speed * Time.deltaTime;
        if (Input.GetAxisRaw("Horizontal") < 0 && canMoveLeft)
            moveX = -speed * Time.deltaTime;
        if (Input.GetAxisRaw("Vertical") > 0 && canMoveUp)
            moveY = speed * Time.deltaTime;
        if (Input.GetAxisRaw("Vertical") < 0 && canMoveDown)
            moveY = -speed * Time.deltaTime;

        previousZLayer = Mathf.RoundToInt(transform.position.z);

        if (Input.GetKeyDown(KeyCode.Q) && canMoveZMinus)
        {
            moveZ = -1;
        }
        if (Input.GetKeyDown(KeyCode.E) && canMoveZPlus)
        {
            moveZ = 1;
        }

        // If moving Z and grounded, snap to ground on new Z
        bool isGrounded = groundChecker != null ? groundChecker.grounded : false;
        bool movingZ = moveZ != 0;

        Vector3 newPos = transform.position + new Vector3(moveX, moveY, moveZ);

        // If stuck in ground after movement, push to closest empty collision spot
        if (IsPositionBlocked(newPos))
        {
            Debug.Log("Player is stuck in ground, searching for empty collision...");
            Vector3? closestFree = FindClosestFreeSpot(newPos);
            if (closestFree != null)
            {
                transform.position = closestFree.Value;
                UpdateSortingOrder();
                Debug.Log("Player pushed to closest open area: " + closestFree);
                movementLocked = true;
                movementLockTimer = movementLockDuration;
                return;
            }
            else
            {
                Debug.LogWarning("No open area found nearby!");
            }
        }
        else
        {
            if (movingZ && isGrounded)
            {
                int newZLayer = Mathf.RoundToInt(newPos.z);
                SnapToGroundOnLayer(newZLayer);
            }
            else
            {
                transform.position = newPos;
            }
        }

        UpdateSortingOrder();

        if (animatorController != null && groundChecker != null)
        {
            animatorController.anim.SetBool("isGrounded", groundChecker.grounded);
        }
    }

    bool IsOverlappingMiddleGround(Vector3Int cell)
    {
        return middleFrontTilemap.GetTile(cell) != null || middleBackTilemap.GetTile(cell) != null;
    }

    bool IsTouching(Vector2 direction)
    {
        if (boxCollider == null) return false;
        float distance = 0.05f;
        RaycastHit2D hit = Physics2D.BoxCast(boxCollider.bounds.center, boxCollider.bounds.size, 0f, direction, distance, LayerMask.GetMask("Default"));
        return hit.collider != null;
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
            if (closestPos != null) break;
        }
        return closestPos;
    }

    void PushToClosestOpenArea(Vector3? intendedTarget = null)
    {
        Vector3 pos = transform.position;
        Vector3 target = intendedTarget ?? pos;

        Vector3? closestFree = FindClosestFreeSpot(target);

        if (closestFree != null)
        {
            transform.position = closestFree.Value;
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
}