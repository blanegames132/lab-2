using System;
using UnityEngine.Tilemaps;

[Serializable]
public class TileData


{

    public TileBase tileAsset;
    public string blockTagOrName;  // Stores either "cave", "air", or biome tag from Inspector (e.g. "grassland", "desert")
}
public static class TileTypeExtensions
{

    /// <summary>
    /// Returns the enum name as a lowercase tag ("air", "cave").
    /// </summary>
    public static string ToTag(this TileType type)
    {

        return type.ToString().ToLower();
    }
}