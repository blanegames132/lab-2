using UnityEngine;

public class PlayerZMovement : MonoBehaviour
{
    [SerializeField] private float zMoveAmount = 0.1f; // How much to move per frame while holding key

    void Update()
    {
        Vector3 pos = transform.position;

        // Move "forward" in Z when holding W
        if (Input.GetKey(KeyCode.W))
        {
            pos.z += zMoveAmount;
        }
        // Move "back" in Z when holding S
        if (Input.GetKey(KeyCode.S))
        {
            pos.z -= zMoveAmount;
        }

        transform.position = pos;
    }
}