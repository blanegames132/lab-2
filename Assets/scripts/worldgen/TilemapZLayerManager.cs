//using UnityEngine;
//using UnityEngine.Tilemaps;

///// <summary>
///// Z layer manager for 3D infinite world. Handles 5 layers: back, middleBack, ground, middleFront, front (Z: -2, -1, 0, 1, 2).
///// Each tilemap is always responsible for its single assigned world Z slice (per frame).
///// Defensive: Only one Z per tilemap ever. Setting a tile at the wrong Z throws.
///// </summary>
//public class TilemapZLayerManager : MonoBehaviour
//{
//    public bool enableZLayering = true;

//    public Tilemap backTilemap, middleBackTilemap, groundTilemap, middleFrontTilemap, frontTilemap;
//    public int zBack = -2, zMiddleBack = -1, zGround = 0, zMiddleFront = 1, zFront = 2;

//    private int lastPlayerZInt = int.MinValue;

//    public System.Action<Tilemap, int> OnSpawnLayer;
//    public System.Action<Tilemap, int> OnDespawnLayer;

//    /// <summary>
//    /// Sets each tilemap's transform Z to its assigned world Z slice, based on current player position.
//    /// Only one Z per tilemap, every frame.
//    /// </summary>
//    public void UpdateLayerZs(Vector3 playerWorldPos)
//    {
//        if (!enableZLayering) return;

//        int playerZInt = Mathf.RoundToInt(playerWorldPos.z);

//        int backZ = playerZInt + zBack;
//        int middleBackZ = playerZInt + zMiddleBack;
//        int groundZ = playerZInt + zGround;
//        int middleFrontZ = playerZInt + zMiddleFront;
//        int frontZ = playerZInt + zFront;

//        backTilemap.transform.position = new Vector3(backTilemap.transform.position.x, backTilemap.transform.position.y, backZ);
//        middleBackTilemap.transform.position = new Vector3(middleBackTilemap.transform.position.x, middleBackTilemap.transform.position.y, middleBackZ);
//        groundTilemap.transform.position = new Vector3(groundTilemap.transform.position.x, groundTilemap.transform.position.y, groundZ);
//        middleFrontTilemap.transform.position = new Vector3(middleFrontTilemap.transform.position.x, middleFrontTilemap.transform.position.y, middleFrontZ);
//        frontTilemap.transform.position = new Vector3(frontTilemap.transform.position.x, frontTilemap.transform.position.y, frontZ);

//        // Spawn/Despawn logic (unchanged)
//        if (lastPlayerZInt == int.MinValue)
//        {
//            lastPlayerZInt = playerZInt;
//            OnSpawnLayer?.Invoke(backTilemap, backZ);
//            OnSpawnLayer?.Invoke(middleBackTilemap, middleBackZ);
//            OnSpawnLayer?.Invoke(groundTilemap, groundZ);
//            OnSpawnLayer?.Invoke(middleFrontTilemap, middleFrontZ);
//            OnSpawnLayer?.Invoke(frontTilemap, frontZ);
//            return;
//        }
//        int delta = playerZInt - lastPlayerZInt;
//        lastPlayerZInt = playerZInt;

//        if (delta == 0) return;

//        if (delta > 0)
//        {
//            OnSpawnLayer?.Invoke(frontTilemap, frontZ);
//            OnDespawnLayer?.Invoke(backTilemap, backZ - delta);
//        }
//        else if (delta < 0)
//        {
//            OnSpawnLayer?.Invoke(backTilemap, backZ);
//            OnDespawnLayer?.Invoke(frontTilemap, frontZ - delta);
//        }
//    }

//    /// <summary>
//    /// Returns the assigned world Z for each tilemap, in order: [back, middleBack, ground, middleFront, front].
//    /// </summary>
//    public int[] GetActiveZs(Vector3 playerWorldPos)
//    {
//        int playerZInt = Mathf.RoundToInt(playerWorldPos.z);
//        return new int[] {
//            playerZInt + zBack,
//            playerZInt + zMiddleBack,
//            playerZInt + zGround,
//            playerZInt + zMiddleFront,
//            playerZInt + zFront
//        };
//    }

//    /// <summary>
//    /// Returns the correct tilemap for a given Z, based on current player position.
//    /// Only one tilemap per Z, guaranteed.
//    /// </summary>
//    public Tilemap GetTilemapForZ(int z, Vector3 playerWorldPos)
//    {
//        int[] zs = GetActiveZs(playerWorldPos);
//        Tilemap[] maps = new[] { backTilemap, middleBackTilemap, groundTilemap, middleFrontTilemap, frontTilemap };
//        for (int i = 0; i < zs.Length; i++)
//            if (z == zs[i])
//                return maps[i];
//        return null;
//    }

//    /// <summary>
//    /// Defensive helper: Throws if you try to set a tile in a tilemap at the wrong Z.
//    /// Always call this before SetTile!
//    /// </summary>
//    public static void SetTileDefensive(Tilemap tilemap, Vector3Int cell)
//    {
//        int tilemapZ = Mathf.RoundToInt(tilemap.transform.position.z);
//        if (cell.z != tilemapZ)
//            throw new System.InvalidOperationException(
//                $"DEFENSIVE: Tilemap '{tilemap.name}' can only set tiles at Z={tilemapZ}, but tried Z={cell.z}"
//            );
//    }
//}