using UnityEngine;
using UnityEngine.Tilemaps;

public class TileBlockClickHandler : MonoBehaviour
{
    public Tilemap groundTilemap;
    public TileHiddenSet tileHiddenSet;
    public Transform playerTransform;

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

            var hideSet = tileHiddenSet.GetTilesToHide(playerTransform.position);

            if (hideSet.Contains(clickedCell))
            {
                if (groundTilemap.HasTile(clickedCell))
                {
                    TileInfiniteCameraSpawner.deletedTiles.Add(clickedCell); // Mark as deleted
                    groundTilemap.SetTile(clickedCell, null);
                    Debug.Log("block added to inventory");
                }
            }
        }
    }
}