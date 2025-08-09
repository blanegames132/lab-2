using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerControle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private isgrounded groundChecker;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TileInfiniteCameraSpawner spawner;
    [SerializeField] private PlayerAnimatorController animatorController;
    [SerializeField] private PlayerMovement playerMovement; // Reference to movement script

    [Header("Tiles & World")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleFrontTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private float worldBottomY = -500f;

    [Header("Movement Lock")]
    [SerializeField] private bool movementLocked = false;
    [SerializeField] private float movementLockTimer = 0f;
    [SerializeField] private float movementLockDuration = 0.05f;

    void Start()
    {
        if (!groundChecker) groundChecker = GetComponent<isgrounded>();
        if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
        if (!spawner) spawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        if (!animatorController) animatorController = GetComponent<PlayerAnimatorController>();
        if (!playerMovement) playerMovement = GetComponent<PlayerMovement>();

        // Clamp logic is now handled by AlwaysStartOnGround.cs!
        UpdateSortingOrder();
    }

    // --- rest of the original code (surface finding, collision, snap to ground, etc.) ---

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

    // ... rest of your non-movement logic ...
}