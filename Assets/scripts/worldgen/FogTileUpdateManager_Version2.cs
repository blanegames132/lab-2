using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Efficiently updates fog overlay tiles, adding/removing only changed tiles.
/// </summary>
public class FogTileUpdateManager : MonoBehaviour
{
    private HashSet<Vector3Int> currentFogTiles = new HashSet<Vector3Int>();

    public void UpdateFogTiles(Tilemap fogTilemap, TileBase fogTile, HashSet<Vector3Int> newFogTiles, Color fogColor)
    {
        if (fogTilemap == null || fogTile == null) return;

        // Remove old fog
        foreach (var pos in currentFogTiles)
        {
            if (!newFogTiles.Contains(pos))
                fogTilemap.SetTile(pos, null);
        }
        // Add new fog
        foreach (var pos in newFogTiles)
        {
            if (!currentFogTiles.Contains(pos))
            {
                fogTilemap.SetTile(pos, fogTile);
                fogTilemap.SetColliderType(pos, Tile.ColliderType.None);
            }
        }
        fogTilemap.color = fogColor;
        currentFogTiles = new HashSet<Vector3Int>(newFogTiles);
    }
}