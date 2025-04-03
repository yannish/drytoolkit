using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipEventConfig))]
public class ClipEventConfigInspector : Editor
{
    private ClipEventConfig config;
    
    private void OnEnable()
    {
        config = (ClipEventConfig)target;
        AnimationMode.StartAnimationMode();
    }

    private void OnDisable() => AnimationMode.StopAnimationMode();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (
            config.clip != null
            && Selection.activeObject != null
            && Selection.activeObject is GameObject gameObject
            )
        {
            // AnimationMode.StartAnimationMode();
            AnimationMode.SampleAnimationClip(
                gameObject, 
                config.clip,
                config.eventTime
            );
            // AnimationMode.StopAnimationMode();
            SceneView.RepaintAll();
        }
    }
}
