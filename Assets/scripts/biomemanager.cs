using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Central place for biome lookup and tag/tile resolution.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    [SerializeField] public List<Biome> biomes = new List<Biome>();

    public TileBase ResolveTileFromTag(int biomeIndex, string tag)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return null;
        if (tag.StartsWith("surface:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].surfaceTile;
        if (tag.StartsWith("subsurface:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].subsurfaceTile;
        if (tag.StartsWith("ground:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].groundTile;
        if (tag.StartsWith("bedrock:", StringComparison.OrdinalIgnoreCase))
            return biomes[biomeIndex].bedrockTile;
        if (tag == "unt1")
            return biomes[biomeIndex].surfaceTile;
        if (tag == "unt2")
            return biomes[biomeIndex].groundTile;
        return null;
    }

    public Biome GetBiome(int biomeIndex)
    {
        if (biomeIndex < 0 || biomeIndex >= biomes.Count)
            return null;
        return biomes[biomeIndex];
    }
}