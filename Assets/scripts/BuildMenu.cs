using UnityEngine;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    public GameObject buildMenuPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && buildMenuPanel != null)
        {
            buildMenuPanel.SetActive(!buildMenuPanel.activeSelf);
        }
    }
}