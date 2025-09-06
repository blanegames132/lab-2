using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Selects and manages the world seed. Allows toggling and re-applying the seed.
/// </summary>
public class SeedSelector : MonoBehaviour
{
    [Header("World Seed (leave blank for random)")]
    [Tooltip("Leave blank for random world. If set, this is your world seed.")]
    public string worldSeed = "";

    [Header("Used Seed (Read-Only)")]
    [ReadOnlyField] public string usedSeedString;
    [ReadOnlyField] public int usedSeedInt;

    [Header("Seed Control")]
    public bool applyRandomSeed = true;
    public bool forceReapplySeed = false;

    private void Awake()
    {
        GenerateSeed(applyRandomSeed || string.IsNullOrWhiteSpace(worldSeed));
    }

    private void OnValidate()
    {
        // Allow toggling in editor
        if (forceReapplySeed)
        {
            GenerateSeed(applyRandomSeed || string.IsNullOrWhiteSpace(worldSeed));
            forceReapplySeed = false;
        }
    }

    /// <summary>
    /// Generates and sets the seed.
    /// </summary>
    /// <param name="random">If true, generate a random seed</param>
    public void GenerateSeed(bool random)
    {
        if (random)
            usedSeedString = Guid.NewGuid().ToString("N");
        else
            usedSeedString = worldSeed;

        usedSeedInt = usedSeedString.GetHashCode();
    }

    /// <summary>
    /// Returns the current seed value as int.
    /// </summary>
    public int CurrentSeedInt => usedSeedInt;

    /// <summary>
    /// Returns the current seed value as string.
    /// </summary>
    public string CurrentSeedString => usedSeedString;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
public class ReadOnlyFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif

/// <summary>
/// Attribute for marking fields as read-only in the inspector.
/// </summary>
public class ReadOnlyFieldAttribute : PropertyAttribute { }