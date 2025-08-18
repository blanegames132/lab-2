using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Overlays fog tiles over every active tile in the procedural world
/// at all specified "back" layers, except for the tile at the player's X position.
/// </summary>
public class GroundFogController : MonoBehaviour
{
    [Header("Target Tilemap (the one to overlay)")]
    public Tilemap targetTilemap;

    [Header("Fog")]
    public Tilemap fogTilemap;
    public TileBase fogTile;

    [Header("Fog Layers")]
    // Which Z offsets from player Z are "back" layers?
    public int[] backLayerZOffsets = new int[] { 2, -2 };

    [Header("Sorting/Rendering")]
    public string fogSortingLayer = "Fog";
    public int fogSortingOrder = 0;

    [Header("Player Reference")]
    public Transform player;

    [Header("Procedural World Reference")]
    public TileInfiniteCameraSpawner worldSpawner;

    void Start()
    {
        var fogTilemapRenderer = fogTilemap.GetComponent<TilemapRenderer>();
        if (fogTilemapRenderer != null)
        {
            fogTilemapRenderer.sortingLayerName = fogSortingLayer;
            fogTilemapRenderer.sortingOrder = fogSortingOrder;
            var pos = fogTilemapRenderer.transform.position;
            fogTilemapRenderer.transform.position = new Vector3(pos.x, pos.y, 0);
        }

        var collider = fogTilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
            collider.enabled = false;
    }

    void Update()
    {
        if (targetTilemap == null || fogTilemap == null || fogTile == null || player == null || worldSpawner == null)
            return;

        fogTilemap.ClearAllTiles();

        int playerZ = Mathf.RoundToInt(player.position.z);
        int playerX = Mathf.RoundToInt(player.position.x);

        // Calculate all Z layers to overlay fog
        HashSet<int> fogZs = new HashSet<int>();
        foreach (var offset in backLayerZOffsets)
            fogZs.Add(playerZ + offset);

        // Get all active tiles from the worldSpawner
        HashSet<Vector3Int> activeTiles = worldSpawner.GetActiveTiles();

        foreach (var tile in activeTiles)
        {
            if (fogZs.Contains(tile.z) && tile.x != playerX)
            {
                Vector3Int fogPos = new Vector3Int(tile.x, tile.y, tile.z);
                fogTilemap.SetTile(fogPos, fogTile);
                fogTilemap.SetColliderType(fogPos, Tile.ColliderType.None);
            }
        }
    }
}