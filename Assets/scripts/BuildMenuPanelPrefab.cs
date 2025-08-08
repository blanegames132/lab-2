using UnityEngine;
using UnityEngine.UI;

public class BuildMenuPanelPrefab : MonoBehaviour
{
    void Awake()
    {
        // Add GridLayoutGroup
        GridLayoutGroup grid = gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(60, 60);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;
        grid.padding = new RectOffset(10, 10, 10, 10);

        // Create 10 white boxes
        for (int i = 0; i < 10; i++)
        {
            GameObject box = new GameObject("WhiteBox" + (i + 1), typeof(Image));
            box.transform.SetParent(transform, false);
            Image img = box.GetComponent<Image>();
            img.color = Color.white;
        }
    }
}