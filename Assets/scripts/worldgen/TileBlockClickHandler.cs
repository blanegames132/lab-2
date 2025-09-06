using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TileBlockClickHandler : MonoBehaviour
{
    public Tilemap groundTilemap;
    public Transform playerTransform;
    public float radius = 3f;

    // Static: persists across scenes; you can move to a manager if you wish
    public static HashSet<Vector3Int> permanentlyDeletedCells = new HashSet<Vector3Int>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (groundTilemap == null || playerTransform == null || Camera.main == null)
            {
                Debug.LogWarning("TileBlockClickHandler: Missing groundTilemap, playerTransform, or Camera.main.");
                return;
            }

            Vector3 mousePos = Input.mousePosition;
            float camToPlayerZ = Mathf.Abs(Camera.main.transform.position.z - playerTransform.position.z);
            mousePos.z = camToPlayerZ;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3 clickCellPos = new Vector3(mouseWorldPos.x, mouseWorldPos.y, playerTransform.position.z);
            Vector3Int clickedCell = groundTilemap.WorldToCell(clickCellPos);

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
                    string tag = null;
                    TileData tileData = null;

                    var archiveManager = FindObjectOfType<WorldArchiveManager>();
                    if (archiveManager != null && archiveManager.worldArchive != null)
                    {
                        tileData = archiveManager.worldArchive.TryGetTile(clickedCell);
                        if (tileData != null && !string.IsNullOrEmpty(tileData.blockTagOrName))
                            tag = tileData.blockTagOrName;
                    }
                    if (tag == null && groundTilemap.GetTile(clickedCell) != null)
                        tag = groundTilemap.GetTile(clickedCell).name;

                    if (string.IsNullOrEmpty(tag) || !tag.StartsWith("bedrock", System.StringComparison.OrdinalIgnoreCase))
                    {
                        TileEventBus.BroadcastTileDelete(groundTilemap, clickedCell);
                        permanentlyDeletedCells.Add(clickedCell); // <--- Mark cell as permanently deleted
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