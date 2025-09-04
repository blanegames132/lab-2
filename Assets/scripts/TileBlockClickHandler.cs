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
    public TileInfiniteCameraSpawner spawner;

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
                    if (spawner == null)
                    {
                        Debug.LogWarning("spawner is not set on TileBlockClickHandler!");
                        return;
                    }

                    // Get biome index for the chunk
                    int biomeIndex = spawner.GetChunkBiome(clickedCell.x / spawner.ChunkSize, clickedCell.z);

                    // Get the tile tag if available from archive
                    string tag = null;
                    var tileData = spawner.worldArchiveManager.TryGetTile(clickedCell);
                    if (tileData != null)
                        tag = tileData.blockTagOrName;

                    if (tag == null || !tag.StartsWith("bedrock", System.StringComparison.OrdinalIgnoreCase))
                    {
                        spawner.worldArchiveManager.DeleteTile(clickedCell, groundTilemap, TileInfiniteCameraSpawner.deletedTiles);
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