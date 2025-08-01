using UnityEngine;

public class camera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //disable camera rotation
        Camera.main.transform.rotation = Quaternion.identity;
        // Set the camera to orthographic mode
        
        // Set the camera size to 5
        Camera.main.orthographicSize = 5f;
        Camera.main.orthographic = false;
        // Set the camera position to (0, 0, -10)
        Camera.main.transform.position = new Vector3(0, 0, -10);
        //dont alow camera to rotate with player
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);


    }

    // Update is called once per frame
    void Update()
    {
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
