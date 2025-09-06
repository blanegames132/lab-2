using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Tiles/SpawnableTileAsset")]
public class TileSpawnAsset : ScriptableObject
{
    public TileBase tileAsset;
    [Tooltip("Relative chance for this tile to spawn (higher = more common)")]
    public float spawnWeight = 1f;
    [Tooltip("Is this tile only allowed above ground?")]
    public bool onlyAboveGround;
    [Tooltip("Is this tile only allowed below ground?")]
    public bool onlyBelowGround;
}