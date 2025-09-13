using drytoolkit.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    [CustomPropertyDrawer(typeof(ViewOnlyAttribute))]
    public class ViewOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(rect, prop, label);
            GUI.enabled = wasEnabled;
        }
    }
}