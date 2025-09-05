using UnityEngine;

public class TilemapZLayerManager : MonoBehaviour
{
    public int groundZ = 0;
    public int frontZ = 1;
    public int middleFrontZ = 2;
    public int middleBackZ = -1;
    public int backZ = -2;

    // Which layer is currently "active" for spawning?
    public enum Layer
    {
        Ground,
        Front,
        MiddleFront,
        MiddleBack,
        Back
    }

    public Layer activeLayer = Layer.Ground;

    // Switches midfront/midback layer if player crosses threshold within tile
    public int GetZForPlayer(Vector3 playerWorldPos, Grid grid, int currentZ)
    {
        Vector3 cellPos = grid.WorldToCell(playerWorldPos);
        Vector3 localPos = grid.WorldToLocal(playerWorldPos);
        float cellFraction = localPos.z - Mathf.Floor(localPos.z); // 0..1 fraction in Z

        // Example: if player is > 0.6 into Z direction, move layer
        if (activeLayer == Layer.MiddleFront && cellFraction > 0.6f)
            activeLayer = Layer.MiddleBack;
        else if (activeLayer == Layer.MiddleBack && cellFraction < 0.4f)
            activeLayer = Layer.MiddleFront;

        switch (activeLayer)
        {
            case Layer.Ground: return groundZ;
            case Layer.Front: return frontZ;
            case Layer.MiddleFront: return middleFrontZ;
            case Layer.MiddleBack: return middleBackZ;
            case Layer.Back: return backZ;
            default: return currentZ; // fallback
        }
    }

    // Utility: set layer directly (optional)
    public void SetActiveLayer(Layer layer)
    {
        activeLayer = layer;
    }
}