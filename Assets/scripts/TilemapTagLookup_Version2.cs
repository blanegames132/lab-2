










using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Enemy is "grounded" if its Y is within yEdgeThreshold of the closest edge (top/bottom) of the tile below in the closest tilemap (by Z).
/// If grounded, disables downward Y movement.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyGroundedByTileYEdge : MonoBehaviour
{
    public TileInfiniteCameraSpawner tileSpawner;
    public float yCheckDistance = 1.1f;     // How far below to check for ground
    public float zThreshold = 1.0f;         // How close in Z to consider a tilemap
    public float yEdgeThreshold = 0.5f;     // How close to tile top/bottom to be grounded

    [Header("DEBUG (Read Only)")]
    public Tilemap debugClosestTilemap;
    public float debugClosestTilemapZ;
    public float debugDistanceToTilemapZ;
    public Vector2 debugTileEdgeY;
    public float debugDistanceToEdge;
    public bool debugIsGrounded;

    private Rigidbody2D rb;

    void Awake()
    {
        if (tileSpawner == null)
            tileSpawner = FindObjectOfType<TileInfiniteCameraSpawner>();
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (tileSpawner == null || tileSpawner.tilemapZSpacings == null) return;

        Vector3 pos = transform.position;
        bool isGrounded = IsGroundedByYEdge(pos, tileSpawner, yCheckDistance, zThreshold, yEdgeThreshold);

        // Block downward Y movement if grounded
        var velocity = rb.linearVelocity;
        if (isGrounded && velocity.y < 0)
            velocity.y = Mathf.Max(0, velocity.y);
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Returns true if enemy Y is within yEdgeThreshold of the closest edge of the tile below in the closest tilemap.
    /// </summary>
    bool IsGroundedByYEdge(Vector3 pos, TileInfiniteCameraSpawner spawner, float yCheck, float zCheck, float yEdgeThresh)
    {
        Tilemap closest = null;
        float closestZ = 0f, minDist = float.MaxValue;
        Vector2 tileEdgeY = Vector2.zero;
        float distToEdge = float.MaxValue;
        bool grounded = false;

        foreach (var spacing in spawner.tilemapZSpacings)
        {
            if (spacing.tilemap == null) continue;
            float dist = Mathf.Abs(pos.z - spacing.zSpacing);
            if (dist > zCheck) continue;
            if (dist < minDist)
            {
                minDist = dist;
                closest = spacing.tilemap;
                closestZ = spacing.zSpacing;
            }
        }

        if (closest == null) { SetDebug(closest, closestZ, minDist, tileEdgeY, distToEdge, false); return false; }

        // Check all integer Y cells from just under the enemy down to yCheck units below
        int yStart = Mathf.FloorToInt(pos.y - 0.1f);
        int yEnd = Mathf.FloorToInt(pos.y - yCheck);
        for (int y = yStart; y >= yEnd; y--)
        {
            Vector3Int cell = closest.WorldToCell(new Vector3(pos.x, y, 0));
            TileBase tile = closest.GetTile(cell);
            if (tile != null)
            {
                // Get tile world bounds (top and bottom Y)
                Vector3 tileWorldPos = closest.CellToWorld(cell);
                Vector3 tileSize = closest.cellSize;
                float tileTop = tileWorldPos.y + tileSize.y;
                float tileBottom = tileWorldPos.y;
                tileEdgeY = new Vector2(tileBottom, tileTop);
                float dTop = Mathf.Abs(pos.y - tileTop);
                float dBottom = Mathf.Abs(pos.y - tileBottom);
                distToEdge = Mathf.Min(dTop, dBottom);
                if (distToEdge <= yEdgeThresh)
                {
                    grounded = true;
                    SetDebug(closest, closestZ, minDist, tileEdgeY, distToEdge, grounded);
                    return true;
                }
            }
        }
        SetDebug(closest, closestZ, minDist, tileEdgeY, distToEdge, grounded);
        return false;
    }

    void SetDebug(Tilemap map, float z, float dz, Vector2 edgeY, float dEdge, bool grounded)
    {
        debugClosestTilemap = map;
        debugClosestTilemapZ = z;
        debugDistanceToTilemapZ = dz;
        debugTileEdgeY = edgeY;
        debugDistanceToEdge = dEdge;
        debugIsGrounded = grounded;
    }
}