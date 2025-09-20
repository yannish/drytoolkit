using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(BindingMapBase), true)]
public class BindingMapPropertyDrawer : PropertyDrawer
{
    // public override VisualElement CreatePropertyGUI(SerializedProperty property)
    // {
    //     // Root container styled like a helpbox
    //     var root = new VisualElement();
    //     root.style.marginBottom = 4;
    //     root.AddToClassList("unity-box"); // helpbox style
    //
    //     // Foldout for collapsible list
    //     var entriesProp = property.FindPropertyRelative("entries");
    //     var foldout = new Foldout
    //     {
    //         text = $"{property.displayName} ({entriesProp.arraySize})",
    //         value = false,
    //     };
    //     foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
    //
    //     root.Add(foldout);
    //
    //     // Draw each entry inside the foldout
    //     for (int i = 0; i < entriesProp.arraySize; i++)
    //     {
    //         SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
    //         var forwardProp = entryProp.FindPropertyRelative("forwardEntry");
    //         var backwardProp = entryProp.FindPropertyRelative("backwardEntry");
    //
    //         // Row container
    //         var row = new VisualElement();
    //         row.style.flexDirection = FlexDirection.Row;
    //         row.style.marginBottom = 2;
    //         // row.style.gap = 4;
    //
    //         // Forward field
    //         var forwardField = new PropertyField(forwardProp);
    //         forwardField.style.flexGrow = 1;
    //
    //         // Backward field
    //         var backwardField = new PropertyField(backwardProp);
    //         backwardField.style.flexGrow = 1;
    //
    //         row.Add(forwardField);
    //         row.Add(backwardField);
    //
    //         foldout.Add(row);
    //     }
    //
    //     // Refresh foldout label if entries change
    //     entriesProp.serializedObject.ApplyModifiedProperties();
    //     foldout.RegisterValueChangedCallback(evt =>
    //     {
    //         foldout.text = $"{property.displayName} ({entriesProp.arraySize})";
    //     });
    //
    //     return root;
    // }
    
    private float LineHeight => EditorGUIUtility.singleLineHeight;
    private const float VerticalSpacing = 2f;
    private const float HeaderPadding = 4f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var entriesProp = property.FindPropertyRelative("entries");

        // Always draw header
        float height = LineHeight + HeaderPadding * 2;

        // If expanded, add space for entries
        if (property.isExpanded && entriesProp != null)
        {
            int count = entriesProp.arraySize;
            height += (LineHeight + VerticalSpacing) * count;
        }

        return height + HeaderPadding * 2; // padding top/bottom
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Draw background helpbox
        GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

        // Original headerRect
        Rect headerRect = new Rect(
            position.x + HeaderPadding,
            position.y,
            position.width - HeaderPadding * 2,
            LineHeight
        );

        EditorGUI.LabelField(headerRect, "BINDING MAP", EditorStyles.boldLabel);

        var bindingMapTag = "BINDING MAP";
        var guiContent = new GUIContent(bindingMapTag);
        var helpBoxStyle = new GUIStyle(EditorStyles.boldLabel);
        var labelSize = helpBoxStyle.CalcSize(guiContent);

        var propNameRect = headerRect;
        propNameRect.x += labelSize.x;
        
        EditorGUI.LabelField(propNameRect,$"[{property.name}]");
        
        var entriesProp = property.FindPropertyRelative("entries");

        var countString = $"COUNT: {entriesProp.arraySize}";
        var countSize = helpBoxStyle.CalcSize(new GUIContent(countString));
        var countRect = headerRect;
        countRect.x += (headerRect.width - countSize.x);
        EditorGUI.LabelField(countRect, countString);
        
        headerRect.y += HeaderPadding;
        
        
        // Adjust for foldout arrow inside the helpbox
        Rect foldoutRect = new Rect(
            headerRect.x + 10f,   // shift right a bit
            headerRect.y + 10f,
            headerRect.width - 10f,
            headerRect.height
        );

        // Draw foldout
        property.isExpanded = EditorGUI.Foldout(
            foldoutRect,
            property.isExpanded,
            GUIContent.none,
            // $"{label.text} ({entriesProp.arraySize})",
            true
            // EditorStyles.foldoutHeader
        );
        
        // Draw entries if expanded
        if (property.isExpanded && entriesProp != null)
        {
            EditorGUI.indentLevel++;
            GUI.enabled = false;
            for (int i = 0; i < entriesProp.arraySize; i++)
            {
                SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                SerializedProperty forwardProp = entryProp.FindPropertyRelative("forwardEntry");
                SerializedProperty backwardProp = entryProp.FindPropertyRelative("backwardEntry");

                Rect rowRect = new Rect(
                    position.x + HeaderPadding,
                    headerRect.yMax + VerticalSpacing + i * (LineHeight + VerticalSpacing),
                    position.width - HeaderPadding * 2,
                    LineHeight
                );

                // Split row into two halves
                float halfWidth = (rowRect.width - 4f) / 2f;
                Rect forwardRect = new Rect(rowRect.x, rowRect.y, halfWidth, rowRect.height);
                Rect backwardRect = new Rect(rowRect.x + halfWidth + 4f, rowRect.y, halfWidth, rowRect.height);

                if (forwardProp.propertyType == SerializedPropertyType.Generic)
                {
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.alignment = TextAnchor.MiddleRight;
                    EditorGUI.LabelField(forwardRect,$"[{forwardProp.boxedValue}]", style);
                }
                else
                {
                    EditorGUI.PropertyField(forwardRect, forwardProp, GUIContent.none, true);
                }

                if (backwardProp.propertyType == SerializedPropertyType.Generic)
                {
                    var style = new GUIStyle(EditorStyles.boldLabel);
                    style.alignment = TextAnchor.MiddleRight;
                    EditorGUI.LabelField(backwardRect, $"[{backwardProp.boxedValue}]", style);
                }
                else
                {
                    EditorGUI.PropertyField(backwardRect, backwardProp, GUIContent.none, true);
                }

            }
            GUI.enabled = true;
            EditorGUI.indentLevel--;
        }
    }
}

//
// [CustomPropertyDrawer(typeof(BindingMapBase), true)]
// public class BindingMapPropertyDrawer : PropertyDrawer
// {
// 	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
// 	{
// 		Color drawColor;
// 		drawColor.a = 0f;
//
// 		var bindingMapTag = "BINDING MAP";
// 		var guiContent = new GUIContent(bindingMapTag);
// 		var helpBoxStyle = new GUIStyle(GUI.skin.box);
// 		var labelSize = helpBoxStyle.CalcSize(guiContent);
// 		
// 		EditorGUI.HelpBox(rect, "", MessageType.None);
// 		int widthMargin = 4;
// 		int heightMargin = 4;
//
// 		Rect headerRect = rect;
// 		headerRect.x += widthMargin;
// 		headerRect.width -= widthMargin * 2f;
// 		headerRect.height = LineHeight;
// 		
// 		// var calculatedLength = GUILayoutUtility.
// 		
// 		EditorGUI.LabelField(headerRect, "BINDING MAP", EditorStyles.boldLabel);
// 		
// 		var specificNameRect = headerRect;
// 		specificNameRect.x += labelSize.x;
// 		specificNameRect.x += widthMargin * 2f;
// 		EditorGUI.LabelField(specificNameRect, $"[{prop.name}]");//, EditorStyles.i);
//
// 		var buttonWidth = 100f;
// 		var buttonRect = headerRect;
// 		buttonRect.width = buttonWidth;
// 		buttonRect.x += (headerRect.xMax);
// 		buttonRect.x -= buttonWidth;
// 		buttonRect.x -= widthMargin * 5.5f;
// 		buttonRect.y += widthMargin;
// 		buttonRect.height -= heightMargin * 2f;
//
// 		var buttonText = prop.isExpanded ? "FOLD" : "SHOW";
// 		if (GUI.Button(buttonRect, buttonText, EditorStyles.miniButton))
// 			prop.isExpanded = !prop.isExpanded;
//
// 		int count = 0;
// 		if (prop.boxedValue != null && prop.boxedValue is IBindable)
// 		{
// 			IBindable bindable = prop.boxedValue as IBindable;
// 			count = bindable.Entries.Count;
// 			if (count == 0 && prop.isExpanded)
// 			{
// 				headerRect.y += LineHeight;
// 				var style = EditorStyles.label;
// 				var prevFontStyle = style.fontStyle;
// 				style.fontStyle = FontStyle.Italic;
// 				EditorGUI.LabelField(headerRect, "... map is empty", style);
// 				style.fontStyle = prevFontStyle;
// 				return;
// 			}
// 		}
//
// 		// int count = 0;
// 		// if (prop.boxedValue != null && prop.boxedValue is IBindable)
// 		// {
// 		// 	IBindable bindable = prop.boxedValue as IBindable;
// 		// 	count = bindable.forward.Count;
// 		// 	if (count == 0 && prop.isExpanded)
// 		// 	{
// 		// 		headerRect.y += LineHeight;
// 		// 		var style = EditorStyles.label;
// 		// 		var prevFontStyle = style.fontStyle;
// 		// 		style.fontStyle = FontStyle.Italic;
// 		// 		EditorGUI.LabelField(headerRect, "... map is empty", style);
// 		// 		style.fontStyle = prevFontStyle;
// 		// 		return;
// 		// 	}
// 		// }
//
// 		if (!prop.isExpanded)
// 			return;
// 		
// 		SerializedProperty entriesProp = prop.FindPropertyRelative("entries");
//
// 		GUI.enabled = false;
// 		Rect drawRect = headerRect;
// 		drawRect.y += LineHeight;
// 		drawRect.y += heightMargin;
// 		
// 		for (int i = 0; i < count; i++)
// 		{
// 			SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
// 			if (entryProp == null)
// 				continue;
//
// 			SerializedProperty forwardProp = entryProp.FindPropertyRelative("forwardEntry");
// 			if (forwardProp != null) 
// 			{
// 				Rect forwardRect = drawRect;
// 				forwardRect.width *= 0.5f;
// 				forwardRect.width -= widthMargin;
// 				forwardRect.height -= heightMargin;
//
// 				EditorGUI.PropertyField(forwardRect, forwardProp, new GUIContent());
// 			}
// 			
// 			SerializedProperty backwardProp = entryProp.FindPropertyRelative("backwardEntry");
// 			if (backwardProp != null)
// 			{
// 				Rect backwardRect = drawRect;
// 				backwardRect.width *= 0.5f;
// 				backwardRect.width -= widthMargin;
// 				backwardRect.height -= heightMargin;
// 				backwardRect.x += drawRect.width * 0.5f;
//
// 				EditorGUI.PropertyField(backwardRect, backwardProp, new GUIContent());
// 			}
// 			drawRect.y += LineHeight;
// 		}
// 		GUI.enabled = true;
// 	}
//
//
// 	private const float heightMargin = 4f;
// 	private float LineHeight => EditorGUIUtility.singleLineHeight + heightMargin;
//
// 	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
// 	{
// 		if (prop.isExpanded && prop.boxedValue != null && prop.boxedValue is IBindable)
// 		{
// 			IBindable bindable = prop.boxedValue as IBindable;
// 			if(bindable == null || bindable.Entries.Count == 0)
// 				return LineHeight * 2f + heightMargin;
//
// 			return LineHeight * (bindable.Entries.Count + 1) + heightMargin;
// 		}
//
// 		return LineHeight + heightMargin * 2f;
// 		// return lineHeight * 2f + heightMargin;
// 	}
// }
