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
    [SerializeField] private Tilemap middlefrontTilemap; // fixed typo: was 'middlefontTilemap'
    [SerializeField] private int zCheckDistance = 1;

    [Header("Spawner Reference")]
    [SerializeField] private TileInfiniteCameraSpawner spawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogging = true;

    // True if player is within 1.5 Z units of a blocking tile behind
    public bool zBlockedBehind { get; private set; } = false;
    // True if player is within 1.7 Z units of a hidden tile in front (this is the only "front" block check)
    public bool zBlockedFrontHidden { get; private set; } = false;

    [Header("Debug Status Logging")]
    [SerializeField] private bool showZBlockStateRepeatedly = false;
    [SerializeField] private float statusLogInterval = 1.0f; // seconds between logs
    private float statusLogTimer = 0f;
    private int statusLogsLeft = 0;
    private const int STATUS_LOG_COUNT = 5;
    private bool[] lastZBlockedStates = new bool[STATUS_LOG_COUNT];
    private bool[] lastZBlockedFrontHiddenStates = new bool[STATUS_LOG_COUNT];
    private int stateIndex = 0;

    void Update()
    {
        Vector3 pos = transform.position;
        bool moved = false;

        // --- Blocked BEHIND ---
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
                Debug.Log("blocked behind");
            }
            else
            {
                Debug.Log("not blocked behind");
            }
        }

        // --- Blocked by (hidden or not) tile in FRONT, at most 1.7 away ---
        zBlockedFrontHidden = false;
        TileBase tileInFront = null;
        string tileInFrontName = "None";
        float zDistFront = 0f;
        if (spawner != null)
        {
            Vector3Int playerCell = middleBackTilemap != null
                ? middleBackTilemap.WorldToCell(transform.position)
                : Vector3Int.FloorToInt(transform.position);
            Vector3Int frontCell = new Vector3Int(playerCell.x, playerCell.y, playerCell.z + zCheckDistance);

            zDistFront = Mathf.Abs(transform.position.z - frontCell.z);

            // Find tile in front, even if hidden
            tileInFront = spawner.GetActualTileAssetAtCell(frontCell);
            tileInFrontName = tileInFront != null ? tileInFront.name : "None";

            // Block if there is ANY tile in front and it's within 1.7 units
            if (tileInFront != null && zDistFront <= 1.7f)
            {
                zBlockedFrontHidden = true;
                Debug.Log($"blocked front: tile '{tileInFrontName}' is {zDistFront} units away (<= 1.7)");
            }
            else
            {
                Debug.Log("not blocked front");
            }

            // Log what tile is in front of the player, even if hidden
            Debug.Log($"Tile in front of player (even if hidden): {tileInFrontName}, Z distance: {zDistFront}");
        }

        // --- Debug Z Blocked Status 5x toggleable logging (for behind and front hidden) ---
        if (showZBlockStateRepeatedly && statusLogsLeft > 0)
        {
            statusLogTimer += Time.deltaTime;
            if (statusLogTimer >= statusLogInterval)
            {
                statusLogTimer = 0f;

                // Store and log both states
                lastZBlockedStates[stateIndex] = zBlockedBehind;
                lastZBlockedFrontHiddenStates[stateIndex] = zBlockedFrontHidden;
                Debug.Log($"[StatusLog {stateIndex + 1}/5] zBlockedBehind: {zBlockedBehind}, zBlockedFrontHidden: {zBlockedFrontHidden}");

                stateIndex++;
                statusLogsLeft--;

                if (statusLogsLeft == 0)
                {
                    showZBlockStateRepeatedly = false; // auto-disable after 5 logs

                    // Show summary of all 5
                    string allStates = "";
                    string allFrontStates = "";
                    for (int i = 0; i < STATUS_LOG_COUNT; i++)
                    {
                        allStates += lastZBlockedStates[i] ? "true" : "false";
                        allFrontStates += lastZBlockedFrontHiddenStates[i] ? "true" : "false";
                        if (i < STATUS_LOG_COUNT - 1)
                        {
                            allStates += ", ";
                            allFrontStates += ", ";
                        }
                    }
                    Debug.Log($"[StatusLog] Last 5 zBlockedBehind states: [{allStates}]");
                    Debug.Log($"[StatusLog] Last 5 zBlockedFrontHidden states: [{allFrontStates}]");
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
                for (int i = 0; i < STATUS_LOG_COUNT; i++)
                {
                    lastZBlockedStates[i] = false;
                    lastZBlockedFrontHiddenStates[i] = false;
                }
                Debug.Log("[StatusLog] Started repeated status logging.");
            }
            else
            {
                Debug.Log("[StatusLog] Stopped repeated status logging.");
            }
        }

        // --- Movement logic: W blocks if any tile is in front and within 1.7 units ---
        if (Input.GetKey(KeyCode.W) && !zBlockedFrontHidden)
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
        else if (Input.GetKey(KeyCode.W) && zBlockedFrontHidden)
        {
            Debug.Log($"Blocked from moving forward in Z because tile '{tileInFrontName}' is {zDistFront} units away (<= 1.7)");
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

        if (moved)
            transform.position = pos;
    }

    private bool IsTileBlocked(Vector3Int zDirection)
    {
        // Use correct tilemap references and fix typo in field name
        Vector3Int playerCell = middleBackTilemap != null
            ? middleBackTilemap.WorldToCell(transform.position)
            : Vector3Int.FloorToInt(transform.position);

        Vector3Int checkPos = playerCell + zDirection * zCheckDistance;

        TileBase tile = null;
        if (middleBackTilemap != null)
            tile = middleBackTilemap.GetTile(checkPos);

        if (tile == null && middlefrontTilemap != null)
            tile = middlefrontTilemap.GetTile(checkPos);

        return tile != null;
    }
}