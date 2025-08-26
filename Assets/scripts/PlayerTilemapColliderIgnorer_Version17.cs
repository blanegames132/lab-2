using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PlayerTilemapColliderIgnorer : MonoBehaviour
{
    [Header("Tilemaps to Ignore")]
    [Tooltip("Assign the Tilemaps whose colliders you want the player to ignore.")]
    public List<Tilemap> tilemapsToIgnore = new List<Tilemap>();

    [Header("Ignore on Start")]
    public bool ignoreOnStart = true;

    void Start()
    {
        if (ignoreOnStart)
            IgnoreTilemapColliders();
    }

    public void IgnoreTilemapColliders()
    {
        Collider2D playerCol2D = GetComponent<Collider2D>();

        foreach (var tm in tilemapsToIgnore)
        {
            if (tm == null) continue;

            // 2D Only
            TilemapCollider2D tmCol2D = tm.GetComponent<TilemapCollider2D>();
            if (tmCol2D != null && playerCol2D != null)
            {
                Physics2D.IgnoreCollision(playerCol2D, tmCol2D, true);
            }
        }
    }
}