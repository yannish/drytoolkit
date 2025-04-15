using System;
using drytoolkit.Runtime.Animation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipConfig))]
public class ClipEventDefinitionInspector : Editor
{
    private ClipConfig config;
    
    private void OnEnable()
    {
        config = (ClipConfig)target;
        AnimationMode.StartAnimationMode();
        Debug.LogWarning("Starting anim mode.");
    }

    private void OnDisable()
    {
        Debug.LogWarning("Stopping anim mode.");
        AnimationMode.StopAnimationMode();
    }

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
