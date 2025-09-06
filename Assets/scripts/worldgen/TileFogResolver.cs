//using UnityEngine;

//public class TileFogResolver : MonoBehaviour
//{
//    [SerializeField] private TileInfiniteCameraSpawner spawner;

//    private string ResolveTile(Vector3Int pos, string fallback, int biomeIndex, bool returnTagOnly)
//    {
//        // --- World archive lookup ---
//        if (spawner != null && spawner.enableWorldArchive
//            && spawner.worldArchiveManager != null
//            && spawner.worldArchiveManager.worldArchive != null)
//        {
//            TileData tileData = spawner.worldArchiveManager.worldArchive.TryGetTile(pos);
//            if (tileData != null)
//                return tileData.blockTagOrName;
//        }

//        // --- Terrain surface calc ---
//        float hillValue = spawner.GetHillValue(pos.x, pos.z);
//        int surfaceY = Mathf.RoundToInt(hillValue * spawner.hillHeight);

//        if (pos.y > surfaceY) return "air";


//        // Use GetBiomeTag from the WorldArchiveManager!
//        string biomeTag = spawner.worldArchiveManager.GetBiomeTag(biomeIndex);

//        if (returnTagOnly)
//            return (pos.y == surfaceY) ? biomeTag : "ground:" + biomeTag;
//        else
//            return (pos.y == surfaceY) ? biomeTag : "ground:" + biomeTag;
//    }

//    // --- Public entry points ---
//    public string GetTileTypeForFog(Vector3Int pos, string fallback, int biomeIndex)
//    {
//        return ResolveTile(pos, fallback, biomeIndex, false);
//    }

//    public string GetTileTagForFog(Vector3Int pos, string fallback, int biomeIndex)
//    {
//        return ResolveTile(pos, fallback, biomeIndex, true);
//    }
//}