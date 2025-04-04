using UnityEditor;
using UnityEngine;

namespace drytoolkit.Runtime.Animation
{
    // [CustomPropertyDrawer(typeof(ClipConfigEvent))]
    public class ClipConfigEventPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUILayout.LabelField("OVERRIDING CLIP EVENT!");
        }
    }
}