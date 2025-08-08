using UnityEngine;

public class BuildMenuToggle : MonoBehaviour
{
    void Update()
    {
        // Toggle visibility when B is pressed
        if (Input.GetKeyDown(KeyCode.B))
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}