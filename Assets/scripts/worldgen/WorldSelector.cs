using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WorldEntry
{
    public string displayName;
    public GameObject worldSpawnerPrefab; // Or a reference to an existing MonoBehaviour if in scene
}

public class WorldSelector : MonoBehaviour
{
    [Header("Available Worlds")]
    [Tooltip("List your world spawners here. Only the selected world will be active.")]
    public List<WorldEntry> availableWorlds = new List<WorldEntry>();
    public int selectedWorldIndex = 0;

    [Header("UI (Optional)")]
    public Dropdown worldDropdown;

    private GameObject activeWorldInstance;

    void Start()
    {
        SetupDropdown();
        ActivateSelectedWorld();
    }

    public void SetupDropdown()
    {
        if (worldDropdown == null || availableWorlds.Count == 0)
            return;

        worldDropdown.ClearOptions();
        List<string> names = new List<string>();
        foreach (var world in availableWorlds)
            names.Add(world.displayName);

        worldDropdown.AddOptions(names);
        worldDropdown.value = selectedWorldIndex;
        worldDropdown.onValueChanged.AddListener(OnWorldDropdownChanged);
    }

    public void OnWorldDropdownChanged(int index)
    {
        selectedWorldIndex = index;
        ActivateSelectedWorld();
    }

    public void ActivateSelectedWorld()
    {
        // Disable previous world
        if (activeWorldInstance != null)
        {
            Destroy(activeWorldInstance);
            activeWorldInstance = null;
        }

        if (selectedWorldIndex < 0 || selectedWorldIndex >= availableWorlds.Count)
            return;

        var entry = availableWorlds[selectedWorldIndex];

        // You could also enable/disable existing objects instead of instantiating
        if (entry.worldSpawnerPrefab != null)
        {
            activeWorldInstance = Instantiate(entry.worldSpawnerPrefab, Vector3.zero, Quaternion.identity);
        }
    }

    // Optional call from outside to select programmatically
    public void SelectWorld(int index)
    {
        if (index < 0 || index >= availableWorlds.Count)
            return;
        selectedWorldIndex = index;
        ActivateSelectedWorld();
        if (worldDropdown != null)
            worldDropdown.value = index;
    }
}