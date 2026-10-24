using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnableIfAttribute))]
public class EnableIfPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EnableIfAttribute enableIf = (EnableIfAttribute)attribute;
        bool enabled = ConditionalAttributeUtils.Evaluate(property, enableIf.conditionField, enableIf.compareValue);

        using (new EditorGUI.DisabledScope(!enabled))
            EditorGUI.PropertyField(position, property, label, true);
    }
}
