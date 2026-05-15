using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StarColorMapper))]
public class StarColorMapperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw every field except the four gradient colors
        DrawPropertiesExcluding(serializedObject, "color0", "color1", "color2", "color3");

        EditorGUILayout.Space(4f);

        // Button sits above the four color fields
        if (GUILayout.Button("Reset to Viridis"))
        {
            serializedObject.FindProperty("color0").colorValue = new Color( 68f / 255f,   1f / 255f,  84f / 255f);
            serializedObject.FindProperty("color1").colorValue = new Color( 49f / 255f, 104f / 255f, 142f / 255f);
            serializedObject.FindProperty("color2").colorValue = new Color( 53f / 255f, 183f / 255f, 121f / 255f);
            serializedObject.FindProperty("color3").colorValue = new Color(253f / 255f, 231f / 255f,  37f / 255f);
        }

        // Draw the four gradient color fields (color0's [Header] attribute draws the section label)
        EditorGUILayout.PropertyField(serializedObject.FindProperty("color0"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("color1"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("color2"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("color3"));

        // Applies changes, registers undo, and marks the object dirty in one call
        serializedObject.ApplyModifiedProperties();
    }
}
