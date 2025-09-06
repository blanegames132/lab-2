using System.Collections.Generic;

/// <summary>
/// Utility to get all biome tags from the Inspector-assigned biomes list.
/// </summary>
public static class BiomeTagEnumerator
{
    public static HashSet<string> GetBiomeTags(List<Biome> biomes)
    {
        var tags = new HashSet<string>();
        if (biomes != null)
        {
            foreach (var biome in biomes)
            {
                if (!string.IsNullOrEmpty(biome.name))
                    tags.Add(biome.name.Trim().ToLower());
            }
        }
        return tags;
    }
}