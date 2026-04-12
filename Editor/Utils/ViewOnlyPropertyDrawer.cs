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
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!ViewOnlyManager.IsVisible(property.serializedObject.targetObject.GetInstanceID()))
                return;

            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = wasEnabled;
        }
    }
}