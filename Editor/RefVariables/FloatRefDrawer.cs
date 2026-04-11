using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(FloatRef))]
public class FloatRefDrawer : PropertyDrawer
{
    const float InputWidth   = 55f;
    const float ToggleOffset = 20f; // 18px btn + 2px marginRight

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var useConstantProp   = property.FindPropertyRelative("useConstant");
        var constantValueProp = property.FindPropertyRelative("constantValue");
        var variableProp      = property.FindPropertyRelative("variable");

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;

        // --- Toggle button ---
        var toggleBtn = new Button();
        toggleBtn.style.width        = 18;
        toggleBtn.style.height       = 18;
        toggleBtn.style.alignSelf    = Align.Center;
        toggleBtn.style.flexShrink   = 0;
        toggleBtn.style.marginRight  = 2;
        toggleBtn.style.paddingLeft  = 0;
        toggleBtn.style.paddingRight = 0;
        toggleBtn.style.fontSize     = 9;

        // --- Constant mode ---
        var constantField = new FloatField(property.displayName);
        constantField.BindProperty(constantValueProp);
        constantField.style.flexGrow = 1;

        // --- Reference mode ---
        var refRow = new VisualElement();
        refRow.style.flexDirection = FlexDirection.Row;
        refRow.style.flexGrow      = 1;

        // FloatField keeps its label so drag-to-scrub works; width is set by GeometryChangedEvent
        var soValueField = new FloatField(property.displayName);
        soValueField.style.flexShrink = 0;
        soValueField.style.flexGrow   = 0;

        var objectField = new ObjectField();
        objectField.objectType        = typeof(FloatVar);
        objectField.allowSceneObjects = false;
        objectField.style.flexGrow    = 1;
        objectField.style.flexShrink  = 1;
        objectField.BindProperty(variableProp);

        refRow.Add(soValueField);
        refRow.Add(objectField);

        // Bind soValueField to the selected FloatVar
        void BindSoValue(FloatVar floatVar)
        {
            soValueField.Unbind();
            if (floatVar != null)
            {
                soValueField.BindProperty(new SerializedObject(floatVar).FindProperty("value"));
                soValueField.SetEnabled(true);
            }
            else
            {
                soValueField.SetValueWithoutNotify(0f);
                soValueField.SetEnabled(false);
            }
        }

        objectField.RegisterValueChangedCallback(evt => BindSoValue(evt.newValue as FloatVar));

        // Correct label widths so [toggle + label] = 40% and [inputs] = 60%
        root.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            if (evt.newRect.width <= 0) return;
            float labelWidth = evt.newRect.width * 0.4f - ToggleOffset;
            constantField.labelElement.style.width = labelWidth;
            soValueField.labelElement.style.width  = labelWidth;
            soValueField.style.width               = labelWidth + InputWidth;
        });

        // Show/hide and update toggle label
        void Refresh(bool isConst)
        {
            toggleBtn.text = isConst ? "C" : "R";
            constantField.style.display = isConst ? DisplayStyle.Flex : DisplayStyle.None;
            refRow.style.display        = isConst ? DisplayStyle.None : DisplayStyle.Flex;
        }

        toggleBtn.clicked += () =>
        {
            useConstantProp.boolValue = !useConstantProp.boolValue;
            property.serializedObject.ApplyModifiedProperties();
            Refresh(useConstantProp.boolValue);
        };

        // Initialize
        BindSoValue(variableProp.objectReferenceValue as FloatVar);
        Refresh(useConstantProp.boolValue);

        root.Add(toggleBtn);
        root.Add(constantField);
        root.Add(refRow);

        return root;
    }
}
