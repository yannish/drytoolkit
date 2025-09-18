using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Utils
{
    public static class EditorUtils
    {
        public static void DrawScriptField(this SerializedObject so)
        {
            GUI.enabled = false;
            SerializedProperty prop = so.FindProperty("m_Script");
            EditorGUILayout.PropertyField(prop, true, new GUILayoutOption[0]);
            GUI.enabled = true;
        }
    }
}