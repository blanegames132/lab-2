using UnityEngine;

public class plyerdealth : MonoBehaviour
{
   // Log player start
    private void Start()
    {
        Debug.Log("Player has started the game");
    }
    // This method is called when the collider enters a trigger
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object that entered the trigger is the player
        if (collision.CompareTag("Player"))
        {
            // Log a message indicating the player has died
            Debug.Log("Player is dead");
            // Optionally, you can add more logic here, like resetting the game or playing a death animation
        }
    }






}




