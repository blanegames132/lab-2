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
    [SerializeField] private Tilemap middleBackTilemap;
    [SerializeField] private Tilemap backTilemap;
    [SerializeField] private int zCheckDistance = 1;

    [Header("Spawner Reference")]
    [SerializeField] private TileInfiniteCameraSpawner spawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    // True if player is within 1.5 Z units of a blocking tile behind
    public bool zBlockedBehind { get; private set; } = false;

    // Debug status toggle and timer/counter for repeated status logging
    [Header("Debug Status Logging")]
    [SerializeField] private bool showZBlockStateRepeatedly = false;
    [SerializeField] private float statusLogInterval = 1.0f; // seconds between logs
    private float statusLogTimer = 0f;
    private int statusLogsLeft = 0;
    private const int STATUS_LOG_COUNT = 5;
    private bool[] lastZBlockedStates = new bool[STATUS_LOG_COUNT];
    private int stateIndex = 0;

    void Update()
    {
        Vector3 pos = transform.position;
        bool moved = false;

        // Check if blocked by tile directly behind (within 1.5 Z units)
        zBlockedBehind = false;
        if (middleBackTilemap != null)
        {
            Vector3Int playerCell = middleBackTilemap.WorldToCell(transform.position);
            Vector3Int behindCell = new Vector3Int(playerCell.x, playerCell.y, playerCell.z - 1);
            TileBase tileBehind = middleBackTilemap.GetTile(behindCell);

            float zDist = Mathf.Abs(transform.position.z - behindCell.z);

            if (tileBehind != null && zDist <= 1.5f)
            {
                zBlockedBehind = true;
                Debug.Log("blocked");
            }
            else
            {
                Debug.Log("not blocked");
            }
        }

        // --- Debug Z Blocked Status 5x toggleable logging ---
        if (showZBlockStateRepeatedly && statusLogsLeft > 0)
        {
            statusLogTimer += Time.deltaTime;
            if (statusLogTimer >= statusLogInterval)
            {
                statusLogTimer = 0f;

                // Store and log state
                lastZBlockedStates[stateIndex] = zBlockedBehind;
                Debug.Log($"[StatusLog {stateIndex + 1}/5] zBlockedBehind: {zBlockedBehind}");

                stateIndex++;
                statusLogsLeft--;

                if (statusLogsLeft == 0)
                {
                    showZBlockStateRepeatedly = false; // auto-disable after 5 logs

                    // Show summary of all 5
                    string allStates = "";
                    for (int i = 0; i < STATUS_LOG_COUNT; i++)
                    {
                        allStates += lastZBlockedStates[i] ? "true" : "false";
                        if (i < STATUS_LOG_COUNT - 1)
                            allStates += ", ";
                    }
                    Debug.Log($"[StatusLog] Last 5 zBlockedBehind states: [{allStates}]");
                }
            }
        }

        // Toggle repeated status logging (Space key)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            showZBlockStateRepeatedly = !showZBlockStateRepeatedly;
            if (showZBlockStateRepeatedly)
            {
                statusLogsLeft = STATUS_LOG_COUNT;
                statusLogTimer = 0f;
                stateIndex = 0;
                for (int i = 0; i < STATUS_LOG_COUNT; i++) lastZBlockedStates[i] = false;
                Debug.Log("[StatusLog] Started repeated status logging.");
            }
            else
            {
                Debug.Log("[StatusLog] Stopped repeated status logging.");
            }
        }

        // Movement logic
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
            else
            {
                pos.z += zMoveAmount;
                moved = true;
            }
        }

        // S movement is only possible if not blocked
        if (Input.GetKey(KeyCode.S) && !zBlockedBehind)
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
            else
            {
                pos.z -= zMoveAmount;
                moved = true;
            }
        }
        // If zBlockedBehind is true and S is pressed, nothing happens

        if (moved)
            transform.position = pos;
    }

    private bool IsTileBlocked(Vector3Int zDirection)
    {
        Vector3Int playerCell = middleBackTilemap.WorldToCell(transform.position);
        Vector3Int checkPos = playerCell + zDirection * zCheckDistance;

        TileBase tile = null;
        if (middleBackTilemap != null)
            tile = middleBackTilemap.GetTile(checkPos);

        if (tile == null && backTilemap != null)
            tile = backTilemap.GetTile(checkPos);

        return tile != null;
    }
}