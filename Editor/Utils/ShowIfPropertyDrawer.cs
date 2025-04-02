using UnityEditor;
using UnityEngine;
using System.Reflection;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property))
            return 0; // Hide the property by setting its height to zero

        return EditorGUI.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        SerializedObject serializedObject = property.serializedObject;
        SerializedProperty conditionProperty = serializedObject.FindProperty(showIf.conditionField);
        SerializedProperty nestedProperty = serializedObject.FindProperty(property.propertyPath);
            // .FindPropertyRelative(showIf.conditionField);
        SerializedProperty extraNestedProperty = nestedProperty.FindPropertyRelative(showIf.conditionField);
            
        if (extraNestedProperty != null)
        {
            Debug.LogWarning("found property through its path!");
        }
            
        // if (nestedProperty != null)
        // {
        //     // Debug.LogWarning("found property, it was nested!");
        // }
        
        //... check "nest" here
        
        //... check "array" here
        
        if (conditionProperty != null)
        {
            if (showIf.compareValue == null)
                return conditionProperty.boolValue; // Default behavior (boolean toggle)

            switch (conditionProperty.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return conditionProperty.boolValue.Equals(showIf.compareValue);
                case SerializedPropertyType.Enum:
                    return conditionProperty.enumValueIndex.Equals((int)showIf.compareValue);
                case SerializedPropertyType.Integer:
                    return conditionProperty.intValue.Equals((int)showIf.compareValue);
                case SerializedPropertyType.Float:
                    return conditionProperty.floatValue.Equals((float)showIf.compareValue);
                case SerializedPropertyType.String:
                    return conditionProperty.stringValue.Equals((string)showIf.compareValue);
            }
        }
        else
        {
            // Try reflection (for private fields, properties, etc.)
            object target = property.serializedObject.targetObject;
            FieldInfo field = target.GetType().GetField(
                showIf.conditionField,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance
                );
            
            if (field != null)
            {
                object fieldValue = field.GetValue(target);
                return fieldValue.Equals(showIf.compareValue ?? true);
            }
        }

        return true; // Default: Show property if condition is invalid
    }
}