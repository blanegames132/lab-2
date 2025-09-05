using UnityEngine;
using System.Collections.Generic;
using System;

public class BiomeManagerFallback : MonoBehaviour
{
    [Header("Biomes (add in inspector or via code)")]
    public List<Biome> biomes = new List<Biome>();

    public Biome GetBiome(string biomeName, int biomeIndex = 0)
    {
        if (!string.IsNullOrEmpty(biomeName))
        {
            foreach (var biome in biomes)
                if (biome != null && biome.name.Equals(biomeName, StringComparison.OrdinalIgnoreCase))
                    return biome;
        }
        if (biomeIndex >= 0 && biomeIndex < biomes.Count)
            return biomes[biomeIndex];
        return null;
    }
}