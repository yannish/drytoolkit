using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DebugReaderRegistry))]
public class DebugReaderRegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Open Debug Reader", GUILayout.Height(32)))
            DebugReaderWindow.Open();
        EditorGUILayout.Space(4);
    }
}
