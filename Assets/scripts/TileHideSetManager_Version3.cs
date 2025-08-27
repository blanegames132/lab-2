using UnityEngine;
using UnityEngine.Tilemaps;

public class TileHideSetManager : MonoBehaviour
{
    [Header("Tile Hide Sets")]
    public TileHiddenSet frontTileHiddenSet;
    public MidFrontTileHiddenSet midFrontTileHiddenSet;

    [Header("Player Reference")]
    public Transform playerTransform;

    [Header("Tilemaps for Front Check")]
    public Tilemap frontTilemap;
    public Tilemap middlefrontTilemap;

    private int frontInspectorRadius;
    private int midFrontInspectorRadius;

    void Start()
    {
        if (frontTileHiddenSet != null)
            frontInspectorRadius = frontTileHiddenSet.bubbleHideRadius;
        if (midFrontTileHiddenSet != null)
            midFrontInspectorRadius = midFrontTileHiddenSet.bubbleHideRadius;
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        // Use frontTilemap for cell conversion if available, else fallback
        Vector3Int playerCell = frontTilemap != null
            ? frontTilemap.WorldToCell(playerTransform.position)
            : (middlefrontTilemap != null
                ? middlefrontTilemap.WorldToCell(playerTransform.position)
                : Vector3Int.FloorToInt(playerTransform.position));

        Vector3Int frontCell = new Vector3Int(playerCell.x, playerCell.y, playerCell.z + 1);

        bool tileInFrontFront = (frontTilemap != null && frontTilemap.GetTile(frontCell) != null);
        bool tileInFrontMiddleFront = (middlefrontTilemap != null && middlefrontTilemap.GetTile(frontCell) != null);

        // If EITHER tilemap has a tile in front, use inspector defaults; else set both to 0
        if (tileInFrontFront || tileInFrontMiddleFront)
        {
            if (frontTileHiddenSet != null)
                frontTileHiddenSet.bubbleHideRadius = frontInspectorRadius;
            if (midFrontTileHiddenSet != null)
                midFrontTileHiddenSet.bubbleHideRadius = midFrontInspectorRadius;
        }
        else
        {
            if (frontTileHiddenSet != null)
                frontTileHiddenSet.bubbleHideRadius = 0;
            if (midFrontTileHiddenSet != null)
                midFrontTileHiddenSet.bubbleHideRadius = 0;
        }
    }
}