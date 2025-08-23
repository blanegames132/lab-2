using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCaveUtilityBack : MonoBehaviour
{
    [Header("Cave Generation Controls")]
    public float caveFrequency = 0.09f;
    public float caveThreshold = 0.5f;

    [Header("Tile Assets")]
    public TileBase visibleCaveTileAsset;

    [Header("Tilemap Reference")]
    public Tilemap tilemap; // Back layer

    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        IsInitialized = true;
    }

    public float CaveGenerator(int x, int y, int z)
    {
        float noiseXY = Mathf.PerlinNoise(x * caveFrequency, y * caveFrequency);
        float noiseYZ = Mathf.PerlinNoise(y * caveFrequency, z * caveFrequency);
        float noiseZX = Mathf.PerlinNoise(z * caveFrequency, x * caveFrequency);
        return (noiseXY + noiseYZ + noiseZX) / 3f;
    }
}
