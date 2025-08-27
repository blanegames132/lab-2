using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles clicking on tilemap blocks and removing the single clicked tile (except bedrock)
/// from the ground tilemap and persistent archive.
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

            // Distance check
            Vector3 playerCellCenter = groundTilemap.CellToWorld(groundTilemap.WorldToCell(playerTransform.position));
            playerCellCenter.z = playerTransform.position.z;
            Vector3 clickedCellCenter = groundTilemap.CellToWorld(clickedCell);
            clickedCellCenter.z = playerTransform.position.z;
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

                    // Check if clicked tile is bedrock
                    int biomeIndex = cameraSpawner.GetChunkBiome(clickedCell.x / cameraSpawner.ChunkSize, clickedCell.z);
                    string tag = cameraSpawner.enableWorldArchive
                        ? cameraSpawner.GetTileTagForFog(clickedCell, null, biomeIndex)
                        : null;

                    if (tag == null || !tag.StartsWith("bedrock", System.StringComparison.OrdinalIgnoreCase))
                    {
                        cameraSpawner.DeleteTile(clickedCell);
                        Debug.Log($"Deleted tile at {clickedCell} (not bedrock)");
                    }
                    else
                    {
                        Debug.Log("Cannot delete bedrock tile!");
                    }
                }
            }
        }
    }
}