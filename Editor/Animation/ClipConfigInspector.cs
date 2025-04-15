using System;
using drytoolkit.Runtime.Animation;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipConfig))]
public class ClipConfigInspector : Editor
{
    private ClipConfig config;
    
    public override void OnInspectorGUI()
    {
        DrawPreviewControls();
        DrawDefaultInspector();
    }

    private void OnEnable()
    {
        config = (ClipConfig)target;
        AnimationMode.StartAnimationMode();
        // Debug.LogWarning("Starting anim mode.");
    }

    private void OnDisable()
    {
        // Debug.LogWarning("Stopping anim mode.");
        AnimationMode.StopAnimationMode();
    }

    public float previewScrubTime;
    private bool isPicking;
    private void DrawPreviewControls()
    {
        if (Application.isPlaying)
            return;

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("PREVIEW:", EditorStyles.boldLabel);
            using (new GUILayout.HorizontalScope())
            {
                if (Selection.activeObject != null && Selection.activeObject is GameObject gameObject)
                {
                    EditorGUI.BeginChangeCheck();
                    previewScrubTime = EditorGUILayout.Slider("Clip Time", previewScrubTime, 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        AnimationMode.SampleAnimationClip(gameObject, config.clip, previewScrubTime);
                        SceneView.RepaintAll();
                    }

                    if (GUILayout.Button("ADD EVENT"))
                    {
                        isPicking = true;
                        EditorGUIUtility.ShowObjectPicker<ClipEventDefinition>(
                            null,
                            false, 
                            "",
                            123456
                            ); // Control ID can be any int
                    }
                    
                    if (isPicking && Event.current.commandName == "ObjectSelectorClosed")
                    {
                        var picked = EditorGUIUtility.GetObjectPickerObject();
                        if (picked is ClipEventDefinition selected)
                        {
                            config.events.Add(new ClipConfig.ClipConfigEvent()
                            {
                                time = previewScrubTime,
                                clipEventDefinition = selected
                            });
                            EditorUtility.SetDirty(config);
                        }
                        isPicking = false;
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("... select an animatable gameObject to preview clip.");
                }
            }
        }
        EditorGUILayout.Space();
    }
}