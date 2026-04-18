using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FloatRef))]
public class FloatRefDrawer : PropertyDrawer
{
    // Using IMGUI (OnGUI) rather than CreatePropertyGUI (UIElements) to avoid
    // Unity 6's PropertyField internal change-tracker calling boxedValue on
    // Generic-typed serialized properties, which logs "Unsupported type FloatRef"
    // every editor frame via Internal_CallUpdateFunctions.
    //
    // Drag-to-scrub on the label is preserved: the toggle button is positioned
    // before the label, and the label is passed into PropertyField (not
    // GUIContent.none), so Unity renders it as a live drag region.
    // EditorGUIUtility.labelWidth is adjusted temporarily so the label + button
    // still land on the standard 40 % column mark.

    const float ValueWidth = 55f;
    const float ValueGap   = 2f;

    private GUIStyle _buttonStyle;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
            _buttonStyle.imagePosition = ImagePosition.ImageOnly;
        }

        var useConstantProp   = property.FindPropertyRelative("useConstant");
        var constantValueProp = property.FindPropertyRelative("constantValue");
        var variableProp      = property.FindPropertyRelative("variable");

        label = EditorGUI.BeginProperty(position, label, property);
        EditorGUI.BeginChangeCheck();

        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Toggle button sits at the very left of the field area.
        float btnWidth = _buttonStyle.fixedWidth + _buttonStyle.margin.right;
        Rect buttonRect = new Rect(
            position.x,
            position.y + _buttonStyle.margin.top,
            btnWidth,
            position.height
        );

        bool useConst = useConstantProp.boolValue;
        if (GUI.Button(buttonRect, useConst ? "C" : "R", _buttonStyle))
            useConstantProp.boolValue = !useConst;

        // Field rect starts right after the button.
        // Shrink labelWidth by btnWidth so the label column still aligns with
        // the standard inspector 40 % mark (btn + label together = 40 %).
        Rect fieldRect = new Rect(
            position.x + btnWidth,
            position.y,
            position.width - btnWidth,
            position.height
        );

        float savedLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = savedLabelWidth - btnWidth;

        if (useConstantProp.boolValue)
        {
            // Passing 'label' (not GUIContent.none) makes IMGUI render the label
            // as a drag region → drag-to-scrub works.
            EditorGUI.PropertyField(fieldRect, constantValueProp, label);
        }
        else
        {
            var floatVar = variableProp.objectReferenceValue as FloatVar;
            if (floatVar != null)
            {
                // [draggable label + live value] | [object field]
                float labelPlusValue = EditorGUIUtility.labelWidth + ValueWidth;
                Rect valueRect  = new Rect(fieldRect.x, fieldRect.y,
                    labelPlusValue, fieldRect.height);
                Rect objectRect = new Rect(fieldRect.x + labelPlusValue + ValueGap, fieldRect.y,
                    fieldRect.width - labelPlusValue - ValueGap, fieldRect.height);

                var varSO = new SerializedObject(floatVar);
                varSO.Update();
                // Again, pass 'label' so the label portion remains draggable.
                EditorGUI.PropertyField(valueRect, varSO.FindProperty("value"), label);
                if (varSO.hasModifiedProperties)
                    varSO.ApplyModifiedProperties();

                EditorGUI.PropertyField(objectRect, variableProp, GUIContent.none);
            }
            else
            {
                // No variable assigned yet — just show the object field with label.
                EditorGUI.PropertyField(fieldRect, variableProp, label);
            }
        }

        EditorGUIUtility.labelWidth = savedLabelWidth;
        EditorGUI.indentLevel = indent;

        if (EditorGUI.EndChangeCheck())
            property.serializedObject.ApplyModifiedProperties();

        EditorGUI.EndProperty();
    }
}
