using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerZMovement : MonoBehaviour
{
    [SerializeField] private float zMoveAmount = 0.1f;
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private int zCheckDistance = 1;

    // This bool is true if blocked (so S movement is not allowed)
    public bool zBlockedBehind { get; private set; } = false;

    void Update()
    {
        Vector3 pos = transform.position;

        // Check if blocked behind (S direction)
        zBlockedBehind = IsTileBlocked(Vector3Int.back);

        // Move "forward" in Z when holding W
        if (Input.GetKey(KeyCode.W))
        {
            pos.z += zMoveAmount;
        }
        // Move "back" in Z when holding S, only if not blocked
        if (Input.GetKey(KeyCode.S))
        {
            if (!zBlockedBehind)
            {
                pos.z -= zMoveAmount;
            }
            else
            {
                Debug.Log("S movement blocked: zBlockedBehind is TRUE, cannot move backward in Z.");
            }
        }

        transform.position = pos;
    }

    private bool IsTileBlocked(Vector3Int zDirection)
    {
        if (middleBackTilemap == null) return false;
        Vector3Int playerCell = middleBackTilemap.WorldToCell(transform.position);
        Vector3Int checkPos = playerCell + zDirection * zCheckDistance;
        return middleBackTilemap.GetTile(checkPos) != null;
    }
}