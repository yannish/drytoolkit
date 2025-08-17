
using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(FloatReference))]
public class FloatReferenceDrawer : PropertyDrawer
{
    /// <summary>
    /// Options to display in the popup to select constant or variable.
    /// </summary>
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


        // Get properties
        SerializedProperty useConstant = property.FindPropertyRelative("UseConstant");
        SerializedProperty constantValue = property.FindPropertyRelative("ConstantValue");
        SerializedProperty variable = property.FindPropertyRelative("Variable");


        EditorGUI.BeginChangeCheck();
        label = EditorGUI.BeginProperty(position, label, property);


        Rect labelRect = new Rect(position);
        labelRect.width *= RefVariablesUtil.labelWidth;
        position.width -= labelRect.width;
        position.x = labelRect.xMax + popupStyle.margin.right;

        EditorGUI.PrefixLabel(labelRect, label);


        // Calculate rect for configuration button
        Rect buttonRect = new Rect(position);
        buttonRect.yMin += popupStyle.margin.top;
        buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
        position.xMin = buttonRect.xMax;


        // Store old indent level and set it to 0, the PrefixLabel takes care of it
        int indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        if (GUI.Button(buttonRect, "", popupStyle))
        {
            // Debug.LogWarning("clicked da buton");
            useConstant.boolValue = !useConstant.boolValue;
        }

        // int result = 0;
        
        // int result = EditorGUI.Popup(
        //     buttonRect,
        //     useConstant.boolValue ? 0 : 1,
        //     popupOptions,
        //     popupStyle
        //     );

        // useConstant.boolValue = result == 0;

        float floatRectWidth = 0.12f;
        Rect floatRect = new Rect(position);
        FloatVariable floatRefVariable = variable.objectReferenceValue as FloatVariable;

        //if(useConstant.boolValue)
        //{
        //	EditorGUI.PropertyField(
        //		position,
        //		constantValue,
        //		GUIContent.none
        //		);

        //	if (EditorGUI.EndChangeCheck())
        //		property.serializedObject.ApplyModifiedProperties();

        //	EditorGUI.indentLevel = indent;
        //	EditorGUI.EndProperty();
        //	return;
        //}

        //if(floatRefVariable == null)
        //{
        //EditorGUI.PropertyField(
        //	position,
        //	variable,
        //	GUIContent.none
        //	);

        //if (EditorGUI.EndChangeCheck())
        //	property.serializedObject.ApplyModifiedProperties();

        //EditorGUI.indentLevel = indent;
        //EditorGUI.EndProperty();
        //return;
        //}

        bool changedScrObjValue = false;

        if (
            !useConstant.boolValue
            && floatRefVariable != null
            )
        {
            float prevValue = floatRefVariable.Value;

            //var positionXMax = position.xMax;
            var oldPositionWidth = position.width;
            position.width *= (1f - floatRectWidth);
            position.width -= popupStyle.margin.right;
            //position.width -= 60f;

            floatRect = new Rect(position);
            floatRect.width = oldPositionWidth - position.width;
            //floatRect.x = position.xMax + 12f;
            //floatRect.width -= 12f;
            position.x = floatRect.xMax + popupStyle.margin.right + 6f;
            position.width -= 6f;

            //floatRect.width = position.width;
            //floatRect.xMin = position.xMax;
            //floatRect.xMin = buttonRect.xMax;
            //floatRect.xMin = 120;

            var newValue = EditorGUI.FloatField(
                floatRect,
                floatRefVariable.Value
                );

            if (newValue != prevValue)
                changedScrObjValue = true;

            floatRefVariable.SetValue(newValue);
        }

        EditorGUI.PropertyField(
            position,
            useConstant.boolValue ? constantValue : variable,
            GUIContent.none
            );

        if (!useConstant.boolValue)
        {
            if (floatRefVariable != null)
            {

            }
        }

        if (changedScrObjValue)
        {
            //Debug.Log("applying change to floarRef variable");
            EditorUtility.SetDirty(variable.objectReferenceValue);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


        if (EditorGUI.EndChangeCheck())
        {
            if (variable != null)
            {
                //variable.serializedObject.ApplyModifiedProperties();
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        //AssetDatabase.Refresh();

        EditorGUI.indentLevel = indent;
        EditorGUI.EndProperty();
    }
}