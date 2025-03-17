using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PooledAttribute))]
public class PooledPropertyDrawer : PropertyDrawer
{
    private const float labelWidth = 0.4f;
    private const float poolLabelWidth = 0.15f;
    
    private readonly string[] popupOptions =
    {
        "Default", "Use Pooling Config"
    };
    
    private GUIStyle popupStyle;
    private GUIStyle fieldStyle;
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (popupStyle == null)
        {
            popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
            // popupStyle = new GUIStyle(EditorStyles.popup);
            popupStyle.imagePosition = ImagePosition.ImageOnly;
        }

        if (fieldStyle == null)
        {
            fieldStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
        }
        
        // EditorGUI.BeginChangeCheck();
        
        // label = EditorGUI.BeginProperty(position, label, property);

        Rect poolLabelRect = new Rect(position);
        poolLabelRect.width *= poolLabelWidth;

        Rect normalLabelRect = new Rect(position);
        normalLabelRect.width *= (labelWidth - poolLabelWidth);
        normalLabelRect.x += poolLabelRect.xMax;
        
        position.width -= (poolLabelRect.width);// + poolLabelWidth);
        position.width -= normalLabelRect.width;
        position.x += (poolLabelRect.width + normalLabelRect.width);//
        // position.x += popupStyle.margin.right;
        // position.x += popupStyle.margin.right;
        // position.x += popupStyle.margin.right;

        // Debug.LogWarning($"popupStyle.margin.right : {popupStyle.margin.right}");
        
        GUIContent modifiedLabel = new GUIContent($"[POOLED] {label.text}");
        // EditorGUI.PrefixLabel(poolLabelRect, modifiedLabel);
        EditorGUI.LabelField(poolLabelRect, modifiedLabel);

        GUIContent normalLabel = new GUIContent("NORMAL");
        EditorGUI.LabelField(normalLabelRect, normalLabel);
        
        EditorGUI.PropertyField(position, property, GUIContent.none);
        
        // Rect normalLabelRect = new Rect(position);
        // normalLabelRect.width *= (labelWidth - poolLabelWidth);
        // normalLabelRect.x = poolLabelRect.xMax;

        // var modifiedLabel = label;

        
        // modifiedLabel.
        // modifiedLabel.text = $"[POOLED] {label.text}";
        // EditorGUI.PrefixLabel(poolLabelRect, new GUIContent("[POOLED]"), EditorStyles.boldLabel);
        
        
        // if (property.objectReferenceValue == null)
        // {
        //     
        // }
        
        // var foundPoolHandle = property.objectReferenceValue
    }
}
