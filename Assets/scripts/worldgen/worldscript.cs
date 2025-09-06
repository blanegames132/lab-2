//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Tilemaps;

//public class WorldGenerator : MonoBehaviour
//{
//    [Header("Spawner (optional)")]
//    [SerializeField] public TileInfiniteCameraSpawner selectedSpawner;

//    [Header("Flat Infinite Fallback")]
//    [SerializeField] public Tilemap groundTilemap;
//    [SerializeField] public TileBase groundTile;
//    [SerializeField] public Transform playerTransform;
//    [SerializeField] public int depth = 20; // how many tiles below player to fill
//    [SerializeField] public int extend = 9999; // infinite left/right

//    private HashSet<Vector3Int> placed = new HashSet<Vector3Int>();

//    void Update()
//    {
//        if (playerTransform == null) return;

//        if (selectedSpawner != null)
//        {
//            // ?? Run spawner-driven generation
//            Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);
//            selectedSpawner.UpdateWorldIfNeeded(playerCell);
//        }
//        else
//        {
//            // ?? Run fallback generation (no spawner)
//            RunFallbackInfinite();
//        }
//    }

//    private void RunFallbackInfinite()
//    {
//        Vector3Int playerCell = Vector3Int.FloorToInt(playerTransform.position);
//        int groundY = playerCell.y - 1;

//        for (int x = playerCell.x - extend; x <= playerCell.x + extend; x++)
//        {
//            for (int y = groundY; y > groundY - depth; y--)
//            {
//                Vector3Int pos = new Vector3Int(x, y, 0);
//                if (!placed.Contains(pos))
//                {
//                    groundTilemap.SetTile(pos, groundTile);
//                    placed.Add(pos);
//                }
//            }
//        }
//    }
//}