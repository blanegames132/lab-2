using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class Biome
{
    public string name; // <-- Required for biome tags!
    public TileBase surfaceTile;
    public TileBase subsurfaceTile;
    public TileBase groundTile;
    public TileBase bedrockTile;
}