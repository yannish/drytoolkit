using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ClampAttribute))]
public class ClampPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ClampAttribute clamp = (ClampAttribute)attribute;

        if (property.propertyType == SerializedPropertyType.Float)
        {
            property.floatValue = EditorGUI.FloatField(position, label, property.floatValue);
            property.floatValue = Mathf.Clamp(property.floatValue, clamp.min, clamp.max);
        }
        else if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = EditorGUI.IntField(position, label, property.intValue);
            property.intValue = Mathf.Clamp(property.intValue, (int)clamp.min, (int)clamp.max);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use Clamp with float or int.");
        }
    }
}