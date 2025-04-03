using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

[CustomPropertyDrawer(typeof(ExpandableAttribute))]
public class ExpandableAttributePropertyDrawer : PropertyDrawer
{
    private Editor _editor = null;

    private AnimBool _animBool;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        EditorGUI.PropertyField(position, property, label, true);
        if (property.objectReferenceValue == null)
            return;

        if (_animBool == null)
        {
            _animBool = new AnimBool(property.isExpanded);
            _animBool.valueChanged.AddListener(() =>
            {
                EditorWindow.focusedWindow.Repaint();
                if (_animBool.faded == 0f)
                {
                    Object.DestroyImmediate(_editor);
                }
            });
        }

        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, GUIContent.none);
        _animBool.target = property.isExpanded;

        if (EditorGUILayout.BeginFadeGroup(_animBool.faded))
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                if(!_editor)
                    Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                _editor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFadeGroup();
        
        EditorGUI.EndProperty();
    }
}
