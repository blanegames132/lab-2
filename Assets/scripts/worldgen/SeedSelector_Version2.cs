using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SeedSelector : MonoBehaviour
{
    [Header("World Seed (leave blank for random)")]
    [Tooltip("Leave blank for random world. If set, this is your world seed.")]
    public string worldSeed = "";

    [Header("Used Seed (Read-Only)")]
    [ReadOnlyField] public string usedSeedString;
    [ReadOnlyField] public int usedSeedInt;

    void Awake()
    {
        if (!string.IsNullOrWhiteSpace(worldSeed))
            usedSeedString = worldSeed;
        else
            usedSeedString = Guid.NewGuid().ToString("N");

        usedSeedInt = usedSeedString.GetHashCode();
    }
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

public class ReadOnlyFieldAttribute : PropertyAttribute { }