using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "World/Biome Asset")]
public class BiomeAsset : ScriptableObject
{
    public string biomeName;
    public TileBase surfaceTile;
    public TileBase subsurfaceTile;
    public TileBase groundTile;
    public TileBase bedrockTile;
}