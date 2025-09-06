using UnityEngine;
using UnityEngine.Tilemaps;

public class SetGroundTilesSolid : MonoBehaviour
{
    public Tilemap groundTilemap;

    void Start()
    {
        if (groundTilemap == null) groundTilemap = GetComponent<Tilemap>();
        groundTilemap.gameObject.AddComponent<TilemapCollider2D>();
    }
}