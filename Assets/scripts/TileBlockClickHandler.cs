using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles clicking on tilemap blocks and removing them from both RAM and the persistent archive.
/// </summary>
public class TileBlockClickHandler : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Transform playerTransform;
    public float radius = 3f; // Radius around player in which blocks can be clicked/removed

    [Header("Reference to your TileInfiniteCameraSpawner")]
    public TileInfiniteCameraSpawner cameraSpawner;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            float camToPlayerZ = Mathf.Abs(Camera.main.transform.position.z - playerTransform.position.z);
            mousePos.z = camToPlayerZ;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3 clickCellPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, playerTransform.position.z);
            Vector3Int clickedCell = groundTilemap.WorldToCell(clickCellPos);

            // Calculate player cell position at player's current z
            Vector3 playerCellCenter = groundTilemap.CellToWorld(groundTilemap.WorldToCell(playerTransform.position));
            playerCellCenter.z = playerTransform.position.z;

            // Calculate distance from player to clicked cell center
            Vector3 clickedCellCenter = groundTilemap.CellToWorld(clickedCell);
            clickedCellCenter.z = playerTransform.position.z; // Project to player's z

            float dist = Vector2.Distance(
                new Vector2(playerCellCenter.x, playerCellCenter.y),
                new Vector2(clickedCellCenter.x, clickedCellCenter.y)
            );

            if (dist <= radius)
            {
                if (groundTilemap.HasTile(clickedCell))
                {
                    if (cameraSpawner == null)
                    {
                        Debug.LogWarning("cameraSpawner is not set on TileBlockClickHandler!");
                        return;
                    }
                    cameraSpawner.DeleteTile(clickedCell);
                    cameraSpawner.RefreshGroundTile(clickedCell); // Ensure visual update from spawner
                    Debug.Log("block added to inventory");
                }
            }
        }
    }
}