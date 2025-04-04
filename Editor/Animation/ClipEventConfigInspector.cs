using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipEvent))]
public class ClipEventConfigInspector : Editor
{
    private ClipEvent config;
    
    private void OnEnable()
    {
        config = (ClipEvent)target;
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
