using UnityEditor;
using UnityEngine;

namespace drytoolkit.Editor.Animation
{
    // [CustomPropertyDrawer(typeof(ClipEventConfig))]
    public class ClipEventConfigPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // EditorGUILayout.LabelField("OVERRIDING CLIP EVENT!");
            EditorGUILayout.PropertyField(property, label);
            
            EditorGUI.BeginProperty(position, label, property);
            if (property.objectReferenceValue == null)
                return;
            
            // Get the target ScriptableObject
            ScriptableObject targetObject = (ScriptableObject)property.objectReferenceValue;
            SerializedObject serializedObject = new SerializedObject(targetObject);
            
            serializedObject.Update();
            
            SerializedProperty clipProp = serializedObject.FindProperty("clip");
            SerializedProperty eventTimeProp = serializedObject.FindProperty("eventTime");

            EditorGUILayout.PropertyField(clipProp);
            EditorGUILayout.PropertyField(eventTimeProp);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
            
            if (
                clipProp.objectReferenceValue !=null 
                && Selection.activeObject != null 
                && Selection.activeObject is GameObject gameObject
                )
            {
                var foundAnimator = gameObject.GetComponentInParent(typeof(Animator));
                if(foundAnimator)
                {
                    // foundAnimator.
                    // Debug.LogWarning($"scrubbing to: {eventTimeProp.floatValue}");
                    AnimationMode.StartAnimationMode();
                    AnimationMode.SampleAnimationClip(
                        gameObject, 
                        clipProp.objectReferenceValue as AnimationClip,
                        eventTimeProp.floatValue
                        );
                    SceneView.RepaintAll();
                    AnimationMode.StopAnimationMode();
                }
                else
                {
                    Debug.LogWarning("... no animator found to scrub");
                }
            }
        }
    }
}
