using Unity.Properties;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(FloatRef))]
public class FloatRefDrawer : PropertyDrawer
{
    /// <summary> Cached style to use to draw the popup button. </summary>
    private GUIStyle popupStyle;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;

        // Find properties
        var useConstantProp = property.FindPropertyRelative("useConstant");
        var constantValueProp = property.FindPropertyRelative("constantValue");
        var variableProp = property.FindPropertyRelative("variable");

        // --- Main float field (constant) ---
        var constantField = new FloatField(property.displayName);//+ " - [const]");
        constantField.BindProperty(constantValueProp);
        constantField.style.flexGrow = 1;
        constantField.labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        constantField.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
        constantField.labelElement.style.flexShrink = 0;
        constantField.style.display = useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;

        
        var modeLabel = new Label();
        modeLabel.text = useConstantProp.boolValue ? "[OVR]" : "[REF]";
        modeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        // modeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        // modeLabel.style.alignSelf = Align.Center;
        
        
        // --- Object field for SO ---
        var objectField = new ObjectField()
        {
            objectType = typeof(FloatVar),
            allowSceneObjects = false
        };
        objectField.BindProperty(variableProp);
        objectField.style.flexGrow = 1;
        objectField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;

        float soInputFieldWidth = 40;
        
        // --- Float field that reflects SO.value ---
        var soValueField = new FloatField(property.displayName);//  + " - [ref]");
        var input = soValueField.Q("unity-text-input"); 
        input.style.minWidth = soInputFieldWidth;
        // input.style.maxWidth = soInputFieldWidth;
        input.style.flexGrow = 1;
        // soValueField.labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
        soValueField.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
        soValueField.style.flexGrow = 1;
        soValueField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;

        // Keep in sync with SO value
        objectField.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue is FloatVar so)
            {
                var soSerialized = new SerializedObject(so);
                var soValueProp = soSerialized.FindProperty("value");

                soValueField.BindProperty(soValueProp);
                soValueField.value = so.value;
                soValueField.SetEnabled(true);

                soValueField.RegisterValueChangedCallback(ev2 =>
                {
                    so.value = ev2.newValue;
                    EditorUtility.SetDirty(so);
                });
            }
            else
            {
                soValueField.SetEnabled(false);
                soValueField.value = 0;
            }
        });


        
        // --- PaneOptions-style toggle button ---
        var imguiButton = new IMGUIContainer(() =>
        {
            if (GUILayout.Button(GUIContent.none, GUI.skin.GetStyle("PaneOptions")))
            {
                useConstantProp.boolValue = !useConstantProp.boolValue;
                
                modeLabel.text = useConstantProp.boolValue ? "[OVR]" : "[REF]";
                modeLabel.style.unityFontStyleAndWeight = useConstantProp.boolValue ? FontStyle.Bold : FontStyle.Normal;
                
                constantField.style.display = useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                objectField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                soValueField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                
                property.serializedObject.ApplyModifiedProperties();
            }
        });
        imguiButton.style.alignSelf = Align.Center;
        imguiButton.style.width = 12;
        
        root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            constantField.labelElement.style.width = root.layout.width * 0.4f - imguiButton.layout.width ;// - imguiButton.style.width.value);
            soValueField.labelElement.style.width = root.layout.width * 0.4f  - imguiButton.layout.width;

            var totalWidth = soValueField.labelElement.style.width.value.value;// + soInputFieldWidth;
            soValueField.style.width = totalWidth;// + soInputFieldWidth;
        });
        
        // var modeButton = new Button()
        // {
        //     text = "⋮",
        //     style =
        //     {
        //         width = 20,
        //         unityTextAlign = TextAnchor.MiddleCenter,
        //         unityFontStyleAndWeight = FontStyle.Bold,
        //     }
        // };
        //
        // modeButton.clicked += () =>
        // {
        //     Debug.LogWarning("CLICKED BUTTON");
        //     
        //     modeButton.Blur();
        //     
        //     useConstantProp.boolValue = !useConstantProp.boolValue;
        //     constantField.style.display = useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        //     objectField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        //     soValueField.style.display = !useConstantProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        //     // constantField.style.visibility = useConstantProp.boolValue ? Visibility.Visible : Visibility.Hidden;
        //     // objectField.style.visibility = !useConstantProp.boolValue ? Visibility.Visible : Visibility.Hidden;
        //     // soValueField.style.visibility = !useConstantProp.boolValue ? Visibility.Visible : Visibility.Hidden;
        //     property.serializedObject.ApplyModifiedProperties();
        //     // Refresh();
        // };
        
        // modeButton.text = "⋮"; // or a gear icon if you want
        // modeButton.style.width = 20;
        // modeButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        // modeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        // modeButton.AddToClassList("unity-pane-options"); // ✅ Unity built-in style

        // root.Add(modeButton);
        root.Add(imguiButton);
        root.Add(constantField);
        root.Add(soValueField);
        root.Add(objectField);
        // root.Add(modeLabel);

        return root;
    }
    
    // public override VisualElement CreatePropertyGUI(SerializedProperty property)
    // {
    //     // Create property container element.
    //     var container = new VisualElement();
    //     container.style.flexDirection = FlexDirection.Row;
    //     container.style.flexGrow = 1.0f;
    //     
    //     // Create property fields.
    //     var useConstantProp = new PropertyField(property.FindPropertyRelative("useConstant"));
    //     var constantValueProp = new PropertyField(property.FindPropertyRelative("constantValue"));
    //     var variableProp = new PropertyField(property.FindPropertyRelative("variable"), property.name);
    //
    //     // Add fields to the container.
    //     container.Add(useConstantProp);
    //     container.Add(constantValueProp);
    //     container.Add(variableProp);
    //
    //     return container;
    // }
    
    // public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    // {
    //     if (popupStyle == null)
    //     {
    //         popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
    //         popupStyle.imagePosition = ImagePosition.ImageOnly;
    //     }
    //     
    //     // Debug.LogWarning($"propWidth: {position.width}");
    //     
    //     SerializedProperty useConstantProp = property.FindPropertyRelative("useConstant");
    //     SerializedProperty constantValueProp = property.FindPropertyRelative("constantValue");
    //     SerializedProperty variableProp = property.FindPropertyRelative("variable");
    //     
    //     FloatVar floatRefVariable = variableProp.objectReferenceValue as FloatVar;
    //     
    //     Rect labelRect = new Rect(position);
    //     labelRect.width *= RefVariablesUtil.labelWidth;
    //     
    //     label = EditorGUI.BeginProperty(position, label, property);
    //
    //     
    //     //... DRAW CONSTANT / REFERENCE TOGGLE:
    //     Rect buttonRect = new Rect(position);
    //     buttonRect.yMin += popupStyle.margin.top;
    //     buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
    //     // position.xMin = buttonRect.xMax;
    //     if (GUI.Button(buttonRect, "", popupStyle))
    //     {
    //         Debug.LogWarning("clicked da buton");
    //         useConstantProp.boolValue = !useConstantProp.boolValue;
    //     }
    //     
    //     
    //     labelRect.width -= buttonRect.width;
    //     labelRect.xMin += buttonRect.width;
    //     // labelRect.xMin -= buttonRect.width;
    //     
    //     // position.width -= labelRect.width;
    //     // position.xMin = labelRect.xMax + popupStyle.margin.right;
    //
    //     //... DRAW PREFIX LABEL:
    //     EditorGUI.PrefixLabel(labelRect, label);
    //     
    //     if (!useConstantProp.boolValue && floatRefVariable != null)
    //     {
    //         var so = new SerializedObject(floatRefVariable);
    //         SerializedProperty valueProp = so.FindProperty("value");
    //         // SerializedProperty valueProp = variableProp.FindPropertyRelative("value");
    //         if(valueProp == null)
    //             Debug.LogWarning("value prop's null");
    //         else
    //         {
    //             EditorGUI.PropertyField(labelRect, valueProp, GUIContent.none);
    //         }
    //     }
    //
    //     // //... DRAW CONSTANT / REFERENCE TOGGLE:
    //     // Rect buttonRect = new Rect(position);
    //     // buttonRect.yMin += popupStyle.margin.top;
    //     // buttonRect.width = popupStyle.fixedWidth + popupStyle.margin.right;
    //     // position.xMin = buttonRect.xMax;
    //     // if (GUI.Button(buttonRect, "", popupStyle))
    //     // {
    //     //     Debug.LogWarning("clicked da buton");
    //     //     useConstantProp.boolValue = !useConstantProp.boolValue;
    //     // }
    //     
    //     
    //     // float floatRectWidth = 0.12f;
    //     // Rect floatRect = new Rect(position);
    //     
    //     // if (
    //     //     !useConstantProp.boolValue
    //     //     && floatRefVariable != null
    //     //     )
    //     // {
    //     //     var oldPositionWidth = position.width;
    //     //     position.width *= (1f - floatRectWidth);
    //     //     position.width -= popupStyle.margin.right;
    //     //
    //     //     floatRect = new Rect(position);
    //     //     floatRect.width = oldPositionWidth - position.width;
    //     //     position.x = floatRect.xMax + popupStyle.margin.right + 6f;
    //     //     position.width -= 6f;
    //     //
    //     //     EditorGUI.PropertyField(floatRect, useConstantProp);
    //     //     
    //     //     // var newValue = EditorGUI.FloatField(
    //     //     //     floatRect,
    //     //     //     floatRefVariable.value
    //     //     // );
    //     //
    //     //     // floatRefVariable.SetValue(newValue);
    //     // }
    //     
    //     EditorGUI.PropertyField(
    //         position,
    //         useConstantProp.boolValue ? constantValueProp : variableProp,
    //         GUIContent.none
    //     );
    //
    //     using (var checkScope = new EditorGUI.ChangeCheckScope())
    //     {
    //         if (checkScope.changed)
    //         {
    //             Debug.LogWarning("check!");
    //         }
    //     }
    //     
    //     EditorGUI.EndProperty();
    // }
}
