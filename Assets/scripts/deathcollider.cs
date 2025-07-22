using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
//tog defult player start position
public class DeathCollider : MonoBehaviour
{
    // This method is called when the collider enters a trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that entered the trigger is the player
        if (collision.CompareTag("Player"))
        {
            // Log a message indicating the player has died
            Debug.Log("Player is dead");
            // Optionally, you can add more logic here, like resetting the game or playing a death animation
            // on player is dead , reset player position to start position

            // Get the player GameObject
            GameObject player = collision.gameObject;
            // Get the PlayerController component
            PlayerControle playerController = player.GetComponent<PlayerControle>();
            // Check if the PlayerController component exists

            if (playerController != null)
            {
                // Reset player position to the start position
                player.transform.position = new Vector3(0, 0, 0); // Change this to your desired start position
                // Optionally reset player velocity
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero; // Reset velocity to zero
                }
                Debug.Log("Player position reset to start position");
            }
            else
            {
                Debug.LogError("PlayerController component not found on player GameObject");
            }
        }



    }
}


