using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(ColorReference))]
public class ColorReferenceDrawer : PropertyDrawer
{
    private readonly string[] popupOptions =
    { "Use Constant", "Use Variable" };

    /// <summary> Cached style to use to draw the popup button. </summary>
    private GUIStyle popupStyle;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (popupStyle == null)
        {
            popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
            popupStyle.imagePosition = ImagePosition.ImageOnly;
        }

        label = EditorGUI.BeginProperty(position, label, property);
        position = EditorGUI.PrefixLabel(position, label);

        EditorGUI.BeginChangeCheck();

        // Get properties
        SerializedProperty useConstant = property.FindPropertyRelative("UseConstant");
        SerializedProperty constantColour = property.FindPropertyRelative("ConstantColor");
        SerializedProperty colourVariable = property.FindPropertyRelative("ColorVariable");

        // Calculate rect for configuration button
        Rect buttonRect = new Rect(position);
        buttonRect.yMin += popupStyle.margin.top;
        buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
        position.xMin = buttonRect.xMax;

        // Store old indent level and set it to 0, the PrefixLabel takes care of it
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        int result = EditorGUI.Popup(
            buttonRect,
            useConstant.boolValue ? 0 : 1,
            popupOptions,
            popupStyle
            );

        useConstant.boolValue = result == 0;

        float floatRectWidth = 0.16f;
        Rect floatRect = new Rect(position);
        ColorVariable temp = colourVariable.objectReferenceValue as ColorVariable;

        if (
            !useConstant.boolValue
            && temp != null
            )
        {
            var oldPositionWidth = position.width;
            position.width *= (1f - floatRectWidth);
            position.width -= popupStyle.margin.right;
            floatRect = new Rect(position);
            floatRect.width = oldPositionWidth - position.width;
            position.x = floatRect.xMax + popupStyle.margin.right;
        }

        EditorGUI.PropertyField(
            position,
            useConstant.boolValue ? constantColour : colourVariable,
            GUIContent.none
            );

        if (!useConstant.boolValue)
        {
            if (temp != null)
            {
                var newColour = EditorGUI.ColorField(
                    floatRect,
                    temp.color
                    );

                temp.color = newColour;
            }
        }

        if (EditorGUI.EndChangeCheck())
            property.serializedObject.ApplyModifiedProperties();

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }

}