using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BindingMapBase), true)]
public class BindingMapPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
	{
		Color drawColor;
		drawColor.a = 0f;

		var bindingMapTag = "BINDING MAP";
		var guiContent = new GUIContent(bindingMapTag);
		var helpBoxStyle = new GUIStyle(GUI.skin.box);
		var labelSize = helpBoxStyle.CalcSize(guiContent);
		
		EditorGUI.HelpBox(rect, "", MessageType.None);
		int widthMargin = 4;
		int heightMargin = 4;

		Rect headerRect = rect;
		headerRect.x += widthMargin;
		headerRect.width -= widthMargin * 2f;
		headerRect.height = LineHeight;
		
		// var calculatedLength = GUILayoutUtility.
		
		EditorGUI.LabelField(headerRect, "BINDING MAP", EditorStyles.boldLabel);
		
		var specificNameRect = headerRect;
		specificNameRect.x += labelSize.x;
		specificNameRect.x += widthMargin * 2f;
		EditorGUI.LabelField(specificNameRect, $"[{prop.name}]");//, EditorStyles.i);

		var buttonWidth = 100f;
		var buttonRect = headerRect;
		buttonRect.width = buttonWidth;
		buttonRect.x += (headerRect.xMax);
		buttonRect.x -= buttonWidth;
		buttonRect.x -= widthMargin * 5.5f;
		buttonRect.y += widthMargin;
		
		buttonRect.height -= heightMargin * 2f;
		// buttonRect.AlignRight(100f);
		// buttonRect.x += widthMargin;

		var buttonText = prop.isExpanded ? "FOLD" : "SHOW";
		if (GUI.Button(buttonRect, buttonText, EditorStyles.miniButton))
			prop.isExpanded = !prop.isExpanded;

		// // prop.isExpanded = 
		// 	EditorGUI.Foldout(headerRect, prop.isExpanded, label, EditorStyles.boldLabel);

		int count = 0;
		if (prop.boxedValue != null && prop.boxedValue is IBindable)
		{
			IBindable bindable = prop.boxedValue as IBindable;
			count = bindable.forward.Count;
			if (count == 0 && prop.isExpanded)
			{
				headerRect.y += LineHeight;
				var style = EditorStyles.label;
				var prevFontStyle = style.fontStyle;
				style.fontStyle = FontStyle.Italic;
				EditorGUI.LabelField(headerRect, "... map is empty", style);
				style.fontStyle = prevFontStyle;
				return;
			}
		}

		if (!prop.isExpanded)
			return;
		
		SerializedProperty entriesProp = prop.FindPropertyRelative("entries");

		GUI.enabled = false;
		Rect drawRect = headerRect;
		drawRect.y += LineHeight;
		drawRect.y += heightMargin;
		
		for (int i = 0; i < count; i++)
		{
			SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
			if (entryProp == null)
				continue;

			SerializedProperty forwardProp = entryProp.FindPropertyRelative("forwardEntry");
			if (forwardProp != null) 
			{
				Rect forwardRect = drawRect;
				forwardRect.width *= 0.5f;
				forwardRect.width -= widthMargin;
				forwardRect.height -= heightMargin;

				EditorGUI.PropertyField(forwardRect, forwardProp, new GUIContent());
			}
			
			SerializedProperty backwardProp = entryProp.FindPropertyRelative("backwardEntry");
			if (backwardProp != null)
			{
				Rect backwardRect = drawRect;
				backwardRect.width *= 0.5f;
				backwardRect.width -= widthMargin;
				backwardRect.height -= heightMargin;
				backwardRect.x += drawRect.width * 0.5f;

				EditorGUI.PropertyField(backwardRect, backwardProp, new GUIContent());
			}
			drawRect.y += LineHeight;
		}
		GUI.enabled = true;
	}


	private const float heightMargin = 4f;
	private float LineHeight => EditorGUIUtility.singleLineHeight + heightMargin;

	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		if (prop.isExpanded && prop.boxedValue != null && prop.boxedValue is IBindable)
		{
			IBindable bindable = prop.boxedValue as IBindable;
			if(bindable == null || bindable.forward.Count == 0)
				return LineHeight * 2f + heightMargin;

			return LineHeight * (bindable.forward.Count + 1) + heightMargin;
		}

		return LineHeight + heightMargin * 2f;
		// return lineHeight * 2f + heightMargin;
	}
}
