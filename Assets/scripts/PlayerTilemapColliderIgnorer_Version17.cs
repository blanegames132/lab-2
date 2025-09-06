using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PlayerTilemapColliderIgnorer : MonoBehaviour
{
    [Header("Tilemaps to Ignore")]
    [Tooltip("Assign the Tilemaps whose colliders you want the player to ignore.")]
    public List<Tilemap> tilemapsToIgnore = new List<Tilemap>();

    [Header("Z Range")]
    [Tooltip("Allowed Z range for detecting closest tile.")]
    public float minZ = -2f;
    public float maxZ = 2f;

    [Tooltip("Max distance from player Z to consider a tile as closest.")]
    public float closestTileZThreshold = 0.5f;

    [Header("TileMultiMapHiddenSet Reference")]
    [Tooltip("Reference to the TileMultiMapHiddenSet component.")]
    public TileMultiMapHiddenSet tileMultiMapHiddenSet;

    // Stores previous closest collider to minimize redundant logs
    private TilemapCollider2D previousClosestCollider = null;

    void Start()
    {
        IgnoreTilemapCollidersExceptClosest();
    }

    void Update()
    {
        IgnoreTilemapCollidersExceptClosest();
    }

    public void IgnoreTilemapCollidersExceptClosest()
    {
        Collider2D playerCol2D = GetComponent<Collider2D>();
        if (playerCol2D == null)
        {
            Debug.LogWarning("[PlayerTilemapColliderIgnorer] Player Collider2D not found!");
            return;
        }

        Vector3 playerPos = transform.position;
        float playerZ = Mathf.Clamp(playerPos.z, minZ, maxZ);

        TilemapCollider2D closestCollider = null;
        float closestDist = float.MaxValue;
        int closestIndex = -1;

        // Find closest tile/collider on all tilemaps in Z range
        for (int i = 0; i < tilemapsToIgnore.Count; i++)
        {
            var tm = tilemapsToIgnore[i];
            if (tm == null) continue;

            TilemapCollider2D tmCol2D = tm.GetComponent<TilemapCollider2D>();
            if (tmCol2D == null) continue;

            BoundsInt bounds = tm.cellBounds;
            foreach (var cellPos in bounds.allPositionsWithin)
            {
                float cellZ = cellPos.z;
                if (cellZ < minZ || cellZ > maxZ) continue;

                TileBase tile = tm.GetTile(cellPos);
                if (tile == null) continue;

                float dist = Mathf.Abs(playerZ - cellZ);
                if (dist < closestDist && dist <= closestTileZThreshold)
                {
                    closestDist = dist;
                    closestCollider = tmCol2D;
                    closestIndex = i;
                }
            }
        }

        // Update bubble hide radii based on which collider is active
        SetBubbleHideRadiusForActiveLayer(closestIndex);

        // Ignore all except closest collider
        foreach (var tm in tilemapsToIgnore)
        {
            if (tm == null) continue;
            TilemapCollider2D tmCol2D = tm.GetComponent<TilemapCollider2D>();
            if (tmCol2D == null) continue;

            bool ignoreThisCollider = tmCol2D != closestCollider;
            Physics2D.IgnoreCollision(playerCol2D, tmCol2D, ignoreThisCollider);

            if (ignoreThisCollider)
            {
                Debug.Log($"[PlayerTilemapColliderIgnorer] Disabled collision: Player <-> {tmCol2D.gameObject.name}");
            }
            else
            {
                if (closestCollider != previousClosestCollider)
                {
                    Debug.Log($"[PlayerTilemapColliderIgnorer] ENABLED collision: Player <-> {tmCol2D.gameObject.name} (closest tilemap, Z distance: {closestDist})");
                }
            }
        }

        previousClosestCollider = closestCollider;
    }

    // Sets bubbleHideRadius for exactly the layers you specified (back: 0-3, midBack: 1-3, ground: 2-3, midFront: 3, front: none)
    void SetBubbleHideRadiusForActiveLayer(int activeIndex)
    {
        if (tileMultiMapHiddenSet == null || tileMultiMapHiddenSet.configs == null) return;

        // Reset all to zero first
        for (int i = 0; i < tileMultiMapHiddenSet.configs.Count; i++)
            tileMultiMapHiddenSet.configs[i].bubbleHideRadius = 0;

        switch (activeIndex)
        {
            case 0: // back
                if (tileMultiMapHiddenSet.configs.Count > 3)
                {
                    tileMultiMapHiddenSet.configs[0].bubbleHideRadius = 2;
                    tileMultiMapHiddenSet.configs[1].bubbleHideRadius = 4;
                    tileMultiMapHiddenSet.configs[2].bubbleHideRadius = 6;
                    tileMultiMapHiddenSet.configs[3].bubbleHideRadius = 8;
                }
                break;
            case 1: // midBack
                if (tileMultiMapHiddenSet.configs.Count > 3)
                {
                    tileMultiMapHiddenSet.configs[1].bubbleHideRadius = 2;
                    tileMultiMapHiddenSet.configs[2].bubbleHideRadius = 4;
                    tileMultiMapHiddenSet.configs[3].bubbleHideRadius = 6;
                }
                break;
            case 2: // ground
                if (tileMultiMapHiddenSet.configs.Count > 3)
                {
                    tileMultiMapHiddenSet.configs[2].bubbleHideRadius = 2;
                    tileMultiMapHiddenSet.configs[3].bubbleHideRadius = 4;
                }
                break;
            case 3: // midFront
                if (tileMultiMapHiddenSet.configs.Count > 3)
                    tileMultiMapHiddenSet.configs[3].bubbleHideRadius = 2;
                break;
            case 4: // front
                // Nothing: all bubbleHideRadius stay 0
                break;
            default:
                // No valid collider, all bubbleHideRadius stay 0
                break;
        }
    }
}