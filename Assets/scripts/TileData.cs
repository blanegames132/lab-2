using System;

[Serializable]
public class TileData
{
    public string blockTagOrName;
    public bool discovered = false; // default not discovered

    public TileData() { }
    public TileData(string tag, bool discovered = false)
    {
        this.blockTagOrName = tag;
        this.discovered = discovered;
    }
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