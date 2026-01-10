using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// [CustomPropertyDrawer(typeof(BindingMapBase), true)]
public class BindingMapDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // Root container styled like a helpbox
        var root = new VisualElement();
        var label = new Label("BINDING MAP");
        root.Add(label);
        return root;
    }
}
