using UnityEditor;
using UnityEngine;

namespace Drydock.Tools
{
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
            // Reserve space for the "POOLED" text
            GUIContent pooledLabel = new GUIContent(" [POOLED] ");

            // Measure the size of the bold text
            Vector2 pooledSize = EditorStyles.boldLabel.CalcSize(pooledLabel);

            // Split the position into two: one for the label + "POOLED", and one for the object field
            Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth + pooledSize.x,
                position.height);

            var width = EditorGUIUtility.fieldWidth;
            // property.wi
            Rect fieldRect = new Rect(
                position.x + labelRect.width - pooledSize.x,
                position.y,
                position.width - EditorGUIUtility.labelWidth, 
                position.height
                );
            
            // Rect fieldRect = new Rect(position.x + labelRect.width, position.y, position.width - labelRect.width, position.height);

            // Draw the original label
            EditorGUI.LabelField(labelRect, label);

            // Draw "POOLED" in bold
            GUIStyle boldStyle = new GUIStyle(EditorStyles.boldLabel);
            EditorGUI.LabelField(
                new Rect(labelRect.x + EditorGUIUtility.labelWidth - pooledSize.x, labelRect.y, pooledSize.x, labelRect.height),
                pooledLabel,
                boldStyle
            );

            // Draw the object field (same as default behavior)
            EditorGUI.ObjectField(fieldRect, property, typeof(GameObject), GUIContent.none);
        }

        // public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        // {
        //     if (popupStyle == null)
        //     {
        //         popupStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
        //         // popupStyle = new GUIStyle(EditorStyles.popup);
        //         popupStyle.imagePosition = ImagePosition.ImageOnly;
        //     }
        //
        //     if (fieldStyle == null)
        //     {
        //         fieldStyle = new GUIStyle(GUI.skin.GetStyle("PaneOptions"));
        //     }
        //     
        //     // EditorGUI.BeginChangeCheck();
        //     
        //     // label = EditorGUI.BeginProperty(position, label, property);
        //
        //     Rect poolLabelRect = new Rect(position);
        //     poolLabelRect.width *= poolLabelWidth;
        //
        //     Rect normalLabelRect = new Rect(position);
        //     normalLabelRect.width *= (labelWidth - poolLabelWidth);
        //     normalLabelRect.x += poolLabelRect.xMax;
        //     
        //     position.width -= (poolLabelRect.width);// + poolLabelWidth);
        //     position.width -= normalLabelRect.width;
        //     position.x += (poolLabelRect.width + normalLabelRect.width);//
        //     // position.x += popupStyle.margin.right;
        //     // position.x += popupStyle.margin.right;
        //     // position.x += popupStyle.margin.right;
        //
        //     // Debug.LogWarning($"popupStyle.margin.right : {popupStyle.margin.right}");
        //     
        //     GUIContent modifiedLabel = new GUIContent($"[POOLED]");
        //     EditorGUI.PrefixLabel(poolLabelRect, modifiedLabel);
        //     // EditorGUI.LabelField(poolLabelRect, modifiedLabel, EditorStyles.boldLabel);
        //
        //     // GUIContent normalLabel = new GUIContent("NORMAL");
        //     // EditorGUI.LabelField(normalLabelRect, normalLabel);
        //
        //     EditorGUI.ObjectField(position, GUIContent.none, property.objectReferenceValue, typeof(GameObject), false);
        //     // EditorGUI.ObjectField(position, property, GUIContent.none);
        //     // EditorGUI.PropertyField(position, property, GUIContent.none);
        //
        //     // Rect normalLabelRect = new Rect(position);
        //     // normalLabelRect.width *= (labelWidth - poolLabelWidth);
        //     // normalLabelRect.x = poolLabelRect.xMax;
        //
        //     // var modifiedLabel = label;
        //
        //
        //     // modifiedLabel.
        //     // modifiedLabel.text = $"[POOLED] {label.text}";
        //     // EditorGUI.PrefixLabel(poolLabelRect, new GUIContent("[POOLED]"), EditorStyles.boldLabel);
        //
        //
        //     // if (property.objectReferenceValue == null)
        //     // {
        //     //     
        //     // }
        //
        //     // var foundPoolHandle = property.objectReferenceValue
        // }
    }
}
