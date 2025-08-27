using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerZMovement : MonoBehaviour
{
    [SerializeField] private float zMoveAmount = 0.1f;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private int zCheckDistance = 1;

    public bool zBlockedBehind { get; private set; } = false;
    public bool zBlockedFront { get; private set; } = false;

    void Update()
    {
        Vector3 pos = transform.position;

        // Check if blocked behind (S) and in front (W)
        zBlockedBehind = IsTileBlocked(Vector3Int.back);
        zBlockedFront = IsTileBlocked(Vector3Int.forward);

        // Move forward in Z (W key)
        if (Input.GetKey(KeyCode.W) && !zBlockedFront)
        {
            pos.z += zMoveAmount;
        }
        // Move backward in Z (S key)
        if (Input.GetKey(KeyCode.S) && !zBlockedBehind)
        {
            pos.z -= zMoveAmount;
        }

        transform.position = pos;
    }

    /// <summary>
    /// Checks the tilemap for a tile at the position offset in Z direction.
    /// </summary>
    private bool IsTileBlocked(Vector3Int zDirection)
    {
        if (middleBackTilemap == null) return false;

        Vector3Int playerCell = middleBackTilemap.WorldToCell(transform.position);
        Vector3Int checkPos = playerCell + zDirection * zCheckDistance;
        return middleBackTilemap.GetTile(checkPos) != null;
    }
}