using drytoolkit.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    [CustomPropertyDrawer(typeof(ViewOnlyAttribute))]
    public class ViewOnlyPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!ViewOnlyManager.IsVisible(property.serializedObject.targetObject.GetInstanceID()))
                return 0f;

            if (!property.isArray)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float h = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                for (int i = 0; i < property.arraySize; i++)
                    h += EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(i), true)
                       + EditorGUIUtility.standardVerticalSpacing;
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ViewOnlyManager.IsVisible(property.serializedObject.targetObject.GetInstanceID()))
                return;

            if (!property.isArray)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
                return;
            }

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded,
                $"{label.text} [{property.arraySize}]", true);

            if (!property.isExpanded)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(true);
            float y = headerRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty elem = property.GetArrayElementAtIndex(i);
                float elemHeight = EditorGUI.GetPropertyHeight(elem, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, elemHeight), elem, true);
                y += elemHeight + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }
    }
}