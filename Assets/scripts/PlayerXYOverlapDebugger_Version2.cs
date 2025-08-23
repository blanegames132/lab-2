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
    [SerializeField] private float zMoveAmount = 0.1f; // How much to move per frame
    [SerializeField] private ZMoveMode zMoveMode = ZMoveMode.Free;

    [Header("Tile Blocking Settings")]
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap backTilemap;
    [SerializeField] private int zCheckDistance = 1;   // Tile units to check in Z direction

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    void Update()
    {
        Vector3 pos = transform.position;
        bool moved = false;

        if (Input.GetKey(KeyCode.W))
        {
            if (zMoveMode == ZMoveMode.BlockedByTile)
            {
                if (IsTileBlocked(Vector3Int.forward))
                {
                    if (debugLogging)
                        Debug.Log($"Z movement blocked (forward) at position: {transform.position}");
                }
                else
                {
                    pos.z += zMoveAmount;
                    moved = true;
                    if (debugLogging)
                        Debug.Log($"Moved forward in Z. New position: {pos}");
                }
            }
            else // Free movement
            {
                pos.z += zMoveAmount;
                moved = true;
            }
        }

        if (Input.GetKey(KeyCode.S))
        {
            if (zMoveMode == ZMoveMode.BlockedByTile)
            {
                if (IsTileBlocked(Vector3Int.back))
                {
                    if (debugLogging)
                        Debug.Log($"Z movement blocked (backward) at position: {transform.position}");
                }
                else
                {
                    pos.z -= zMoveAmount;
                    moved = true;
                    if (debugLogging)
                        Debug.Log($"Moved backward in Z. New position: {pos}");
                }
            }
            else // Free movement
            {
                pos.z -= zMoveAmount;
                moved = true;
            }
        }

        if (moved)
            transform.position = pos;
    }

    private bool IsTileBlocked(Vector3Int zDirection)
    {
        Vector3Int playerCell = Vector3Int.FloorToInt(transform.position);
        Vector3Int checkPos = playerCell + zDirection * zCheckDistance;

        TileBase tile = null;
        if (middleBackTilemap != null)
            tile = middleBackTilemap.GetTile(checkPos);

        if (tile == null && backTilemap != null)
            tile = backTilemap.GetTile(checkPos);

        return tile != null;
    }
}