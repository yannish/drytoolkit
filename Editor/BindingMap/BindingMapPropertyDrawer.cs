using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BindingMapBase), true)]
public class BindingMapPropertyDrawer : PropertyDrawer
{
	public override void OnGUI(Rect rect, SerializedProperty prop, GUIContent label)
	{

		Color drawColor;
		drawColor.a = 0f;

		EditorGUI.HelpBox(rect, "", MessageType.None);
		int widthMargin = 4;

		Rect headerRect = rect;
		headerRect.x += widthMargin;
		headerRect.height = lineHeight;
		EditorGUI.LabelField(headerRect, "BINDING MAP", EditorStyles.boldLabel);

		int count = 0;
		if (prop.boxedValue != null && prop.boxedValue is IBindable)
		{
			IBindable bindable = prop.boxedValue as IBindable;
			count = bindable.forward.Count;
			if (count == 0)
			{
				headerRect.y += lineHeight;
				EditorGUI.LabelField(headerRect, "... map is empty");
				return;
			}
		}


		SerializedProperty entriesProp = prop.FindPropertyRelative("entries");

		GUI.enabled = false;
		Rect drawRect = headerRect;
		drawRect.y += lineHeight;
		for (int i = 0; i < count; i++)
		{
			SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
			if (entryProp == null)
				continue;

			SerializedProperty forwardProp = entryProp.FindPropertyRelative("forwardEntry");
			if (forwardProp != null) 
				// if (forwardProp != null && forwardProp.objectReferenceValue != null)
			{
				Rect forwardRect = drawRect;
				forwardRect.width *= 0.5f;
				forwardRect.width -= widthMargin;
				forwardRect.height -= heightMargin;

				EditorGUI.PropertyField(forwardRect, forwardProp, new GUIContent());
				// EditorGUI.ObjectField(forwardRect, forwardProp, GUIContent.none);
			}
			

			SerializedProperty backwardProp = entryProp.FindPropertyRelative("backwardEntry");
			if (backwardProp != null)// && forwardProp.objectReferenceValue != null)
			{
				Rect backwardRect = drawRect;
				backwardRect.width *= 0.5f;
				backwardRect.width -= widthMargin;
				backwardRect.height -= heightMargin;
				backwardRect.x += drawRect.width * 0.5f;

				EditorGUI.PropertyField(backwardRect, backwardProp, new GUIContent());
				// EditorGUI.ObjectField(backwardRect, backwardProp, GUIContent.none);
			}
			drawRect.y += lineHeight;
			
		}
		GUI.enabled = true;
	}


	private const float heightMargin = 2f;
	private float lineHeight => EditorGUIUtility.singleLineHeight + heightMargin;

	public override float GetPropertyHeight(SerializedProperty prop, GUIContent label)
	{
		if (prop.boxedValue != null && prop.boxedValue is IBindable)
		{
			IBindable bindable = prop.boxedValue as IBindable;
			if(bindable == null || bindable.forward.Count == 0)
				return lineHeight * 2f + heightMargin;

			return lineHeight * (bindable.forward.Count + 1) + heightMargin;
		}

		return lineHeight * 2f + heightMargin;
	}
}
