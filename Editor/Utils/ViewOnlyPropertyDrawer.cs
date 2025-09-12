using drytoolkit.Runtime.Utils;
using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    [CustomPropertyDrawer(typeof(ViewOnly))]
    public class ViewOnlyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            base.OnGUI(position, property, label);
            GUI.enabled = true;
        }
    }
}