using UnityEngine;
using UnityEngine.UI;

public class BuildMenuInitializer : MonoBehaviour
{
    public Canvas canvas;
    public GameObject buildMenuPanelPrefab;

    void Start()
    {
        // Create the build menu panel in the top right corner
        GameObject buildMenuPanel = Instantiate(buildMenuPanelPrefab, canvas.transform);

        // Anchor to top right
        RectTransform rect = buildMenuPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20); // 20px from top right

        buildMenuPanel.SetActive(false); // Start hidden
    }
}