using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerZMovementFlexible : MonoBehaviour
{
    public enum ZMoveMode
    {
        Free,
        BlockedByTile
    }

    [Header("General Movement")]
    [SerializeField] private float zMoveAmount = 0.1f;
    [SerializeField] private ZMoveMode zMoveMode = ZMoveMode.Free;

    [Header("Tile Blocking Settings")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap middlefrontTilemap;
    [SerializeField] private Tilemap backTilemap;
    [SerializeField] private Tilemap frontTilemap;
    [SerializeField] private int zCheckDistance = 1;

    [Header("Spawner Reference")]
    [SerializeField] private InfiniteCameraSpawnerModular spawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    public bool zBlockedBehind { get; private set; } = false;
    public bool zBlockedFront { get; private set; } = false;

    private Tilemap[] allTilemaps;

    void Awake()
    {
        allTilemaps = new Tilemap[] {
            groundTilemap,
            middleBackTilemap,
            middlefrontTilemap,
            backTilemap,
            frontTilemap
        };
    }

    void Update()
    {
        Vector3 pos = transform.position;
        bool moved = false;

        // Blocked BEHIND (negative Z direction)
        zBlockedBehind = IsAnyTileBlocked(Vector3Int.back);

        // Blocked FRONT (positive Z direction)
        zBlockedFront = IsAnyTileBlocked(Vector3Int.forward);

        // Debug logging
        if (debugLogging)
        {
            Debug.Log($"zBlockedBehind: {zBlockedBehind}, zBlockedFront: {zBlockedFront}, Z: {pos.z}");
        }

        // Move forward in Z (W key)
        if (Input.GetKey(KeyCode.W) && !zBlockedFront && pos.z + zMoveAmount <= 2f)
        {
            pos.z += zMoveAmount;
            moved = true;
            if (debugLogging) Debug.Log($"Moved forward in Z. New position: {pos}");
        }
        // Move backward in Z (S key)
        if (Input.GetKey(KeyCode.S) && !zBlockedBehind && pos.z - zMoveAmount >= -2f)
        {
            pos.z -= zMoveAmount;
            moved = true;
            if (debugLogging) Debug.Log($"Moved backward in Z. New position: {pos}");
        }

        // Clamp Z position in case anything tries to set it out of bounds
        if (pos.z > 2f) pos.z = 2f;
        if (pos.z < -2f) pos.z = -2f;

        if (moved)
            transform.position = pos;
        else
            // Always clamp anyway, in case something else moved us
            transform.position = new Vector3(pos.x, pos.y, Mathf.Clamp(pos.z, -2f, 2f));
    }

    /// <summary>
    /// Checks all tilemaps for a tile at the position offset in Z direction, at 0 and 1 unit away.
    /// Blocks if any tile is found at either offset.
    /// </summary>
    private bool IsAnyTileBlocked(Vector3Int zDirection)
    {
        Vector3Int playerCell = Vector3Int.FloorToInt(transform.position);

        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap == null) continue;
            // Check 0 units away (current Z cell)
            TileBase tile0 = tilemap.GetTile(playerCell);
            if (tile0 != null) return true;

            // Check 1 unit away (in zDirection)
            Vector3Int checkPos = playerCell + zDirection * zCheckDistance;
            TileBase tile1 = tilemap.GetTile(checkPos);
            if (tile1 != null) return true;
        }
        return false;
    }
}