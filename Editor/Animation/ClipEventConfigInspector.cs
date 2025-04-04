using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipEventDefinition))]
public class ClipEventConfigInspector : Editor
{
    private ClipEventDefinition config;
    
    private void OnEnable()
    {
        config = (ClipEventDefinition)target;
        AnimationMode.StartAnimationMode();
    }

    private void OnDisable() => AnimationMode.StopAnimationMode();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        // if (
        //     config.clip != null
        //     && Selection.activeObject != null
        //     && Selection.activeObject is GameObject gameObject
        //     )
        // {
        //     // AnimationMode.StartAnimationMode();
        //     AnimationMode.SampleAnimationClip(
        //         gameObject, 
        //         config.clip,
        //         config.eventTime
        //     );
        //     // AnimationMode.StopAnimationMode();
        //     SceneView.RepaintAll();
        // }
    }
}
