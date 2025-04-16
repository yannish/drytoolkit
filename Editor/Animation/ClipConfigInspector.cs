using System.Linq;
using UnityEditor;
using UnityEngine;
using drytoolkit.Runtime.Animation;


[CustomEditor(typeof(ClipConfig))]
public class ClipConfigInspector : Editor
{
    private ClipConfig config;

    
    [MenuItem("Tools/BigCheck")]
    public static void FindAllTheseInspectors()
    {
        var all = Resources.FindObjectsOfTypeAll<ClipConfigInspector>();
        Debug.LogWarning($"Found: {all.Length}");
    }
    
    // [MenuItem("Tools/AnimBoolCheck")]
    // public static void FindAllAnimBools()
    // {
    //     var all = Resources.FindObjectsOfTypeAll<ClipConfigInspector>();
    //     Debug.LogWarning($"Found: {all.Length}");
    // }
    
    public override void OnInspectorGUI()
    {
        DrawPreviewControls();
        DrawDefaultInspector();
    }

    private void OnEnable()
    {
        config = (ClipConfig)target;
        // AnimationMode.StartAnimationMode();
        // Debug.LogWarning("Starting anim mode.");
    }

    private void OnDisable()
    {
        // Debug.LogWarning("Stopping anim mode.");
        // AnimationMode.StopAnimationMode();
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
                var foundAnimatorObject = Selection.gameObjects
                    .Select(t => t.GetComponentInChildren(typeof(Animator)))
                    .FirstOrDefault();
                
                if (
                    foundAnimatorObject != null
                    // Selection.activeTransform != null 
                    // && Selection.activeTransform.gameObject is GameObject gameObject
                    )
                {
                    EditorGUI.BeginChangeCheck();
                    previewScrubTime = EditorGUILayout.Slider("Clip Time", previewScrubTime, 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        AnimationMode.SampleAnimationClip(foundAnimatorObject.gameObject, config.clip, previewScrubTime);
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