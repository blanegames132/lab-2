using UnityEngine;
using UnityEngine.Tilemaps;

public class isgrounded : MonoBehaviour
{
    public bool grounded = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        //Tile map[ ====== ground]using UnityEngine.Tilemaps;
        
            // If player collides with an object tilemap "Ground", set grounded to true
            if (collision.gameObject.GetComponent<Tilemap>() != null)
          
                    
                    

            {
            grounded = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // If player leaves collision with an object tagged "Ground", set grounded to false
        if (collision.gameObject.GetComponent<Tilemap>() != null)
        {
            grounded = false;
        }
    }
}