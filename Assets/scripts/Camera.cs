using Unity.VisualScripting;
using UnityEngine;

public class camera : MonoBehaviour
{

    void Start()
    {
        //get Camera rotaiton
            
        Vector3 CameraRotation = transform.rotation.eulerAngles;
        // Print the camera rotation to the console





    }

    // Update is called once per frame
    void Update()
    {
        //set Camera rotation to 0,0,0
        transform.rotation = Quaternion.Euler(0, 0, 0);


    }
}
