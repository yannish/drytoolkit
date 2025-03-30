using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ExpandableAttribute))]
public class ExpandableAttributePropertyDrawer : PropertyDrawer
{
    Editor _editor = null;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property, label, true);
        if (property.objectReferenceValue == null)
            return;

        if (property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none))
        {
            EditorGUI.indentLevel++;
            if(!_editor)
                Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
            _editor.OnInspectorGUI();
            EditorGUI.indentLevel--;
            // using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            // {
            // }
            // using (new EditorGUI.IndentLevelScope()){}
        }
    }
}
