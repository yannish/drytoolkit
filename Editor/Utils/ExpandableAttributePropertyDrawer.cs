using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

[CustomPropertyDrawer(typeof(ExpandableAttribute))]
public class ExpandableAttributePropertyDrawer : PropertyDrawer
{
    [InitializeOnLoadMethod]
    public static void ExpandableCleanupHandler()
    {
        Selection.selectionChanged -= CleanupExpandedEditors;
        Selection.selectionChanged += CleanupExpandedEditors;
    }

    private static void CleanupExpandedEditors()
    {
        foreach (var kvp in idToEditorLookup)
        {
            Editor.DestroyImmediate(kvp.Value);
        }
        idToEditorLookup.Clear();
    }
    

    private static readonly Dictionary<int, Editor> idToEditorLookup = new Dictionary<int, Editor>();

    private AnimBool _animBool;

    private Editor GetEditor(SerializedProperty property)
    {
        int instanceID = property.objectReferenceValue.GetInstanceID();
        if(idToEditorLookup.TryGetValue(instanceID, out var editorCache))
            return editorCache;
        
        var newEditor = Editor.CreateEditor(property.objectReferenceValue, null);

        idToEditorLookup.Add(instanceID, newEditor);
        
        return newEditor;
    }

    private void TryDisposeEditor(int instanceID)
    {
        if (idToEditorLookup.TryGetValue(instanceID, out var editorCache))
        {
            Editor.DestroyImmediate(editorCache);
            idToEditorLookup.Remove(instanceID);
        }
    }
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PropertyField(position, property, label, true);
        if (property.objectReferenceValue == null)
            return;

        int instanceID = property.objectReferenceValue.GetInstanceID();
        EditorGUI.BeginChangeCheck();
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
        if (_animBool == null)
        {
            _animBool = new AnimBool(property.isExpanded);
            _animBool.valueChanged.AddListener(() =>
            {
                EditorWindow.focusedWindow.Repaint();
                if (_animBool.faded == 0f)
                {
                    TryDisposeEditor(instanceID);
                }
            });
        }
        _animBool.target = property.isExpanded;

        if (EditorGUILayout.BeginFadeGroup(_animBool.faded))
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                var cachedEditor = GetEditor(property);
                cachedEditor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFadeGroup();
        EditorGUI.EndProperty();
    }
}
