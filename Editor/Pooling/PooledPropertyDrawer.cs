using UnityEditor;
using UnityEngine;

namespace Drydock.Tools
{
    [CustomPropertyDrawer(typeof(PooledAttribute))]
    public class PooledPropertyDrawer : PropertyDrawer
    {
        private static readonly GUIContent PooledTag = new GUIContent("[POOLED]");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            // IndentedRect shifts x right by (indentLevel * 15px) — the same offset
            // EditorGUI methods apply internally. Drawing from here puts us flush with
            // standard fields at any nesting depth.
            Rect indented          = EditorGUI.IndentedRect(position);
            float labelColumnWidth = EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15f;

            float tagWidth = EditorStyles.boldLabel.CalcSize(PooledTag).x;
            Rect nameRect  = new Rect(indented.x,                      position.y, labelColumnWidth - tagWidth,          position.height);
            Rect tagRect   = new Rect(indented.x + nameRect.width,      position.y, tagWidth,                            position.height);
            Rect fieldRect = new Rect(indented.x + labelColumnWidth,    position.y, indented.width - labelColumnWidth,   position.height);

            // Zero out indent — rects are already placed at the correct indented origin.
            int savedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.LabelField(nameRect, label);
            EditorGUI.LabelField(tagRect, PooledTag, EditorStyles.boldLabel);
            EditorGUI.ObjectField(fieldRect, property, fieldInfo.FieldType, GUIContent.none);

            EditorGUI.indentLevel = savedIndent;
            EditorGUI.EndProperty();
        }
    }
}
