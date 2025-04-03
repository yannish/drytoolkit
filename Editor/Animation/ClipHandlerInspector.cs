using drytoolkit.Runtime.Animation;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ClipHandler))]
public class ClipHandlerInspector : Editor
{
    GUIContent playFromStart;
    const float buttonWidth = 24f;
    
    private void OnEnable()
    {
        playFromStart = EditorGUIUtility.IconContent("d_PlayButton");
    }
    
    public override void OnInspectorGUI()
    {
        DrawPlayControls();
        DrawDefaultInspector();
    }

    private void DrawPlayControls()
    {
        if (!Application.isPlaying)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.LabelField("Enter playmode to enable playback controls.");
            return;
        }

        var clipHandler = target as ClipHandler;
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("STATE CLIPS:", EditorStyles.boldLabel);
            for (int i = 0; i < clipHandler.stateClips.Count; i++)
            {
                var clipConfig = clipHandler.stateClips[i];
                
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        var effectiveBlendTime = clipConfig.overrideBlendInTime
                            ? clipConfig.blendInTime
                            : clipHandler.blendInTime;
                        
                        clipHandler.animSystem.TransitionToState(clipConfig);
                        
                        if(clipHandler.logDebug)
                            Debug.LogWarning($"Transitioning to: {clipConfig.clip.name} in {effectiveBlendTime}");
                    }
                    EditorGUILayout.LabelField(clipConfig.clip.name);
                }
            }
        }
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("ONE SHOT CLIPS:", EditorStyles.boldLabel);
            for (int i = 0; i < clipHandler.oneShotClips.Count; i++)
            {
                var oneShotClipConfig = clipHandler.oneShotClips[i];
                
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        // var effectiveBlendTime = clipConfig.overrideBlendInTime
                        //     ? clipConfig.blendInTime
                        //     : clipHandler.blendInTime;
                        
                        clipHandler.animSystem.PlayOneShot(oneShotClipConfig);
                        
                        if(clipHandler.logDebug)
                            Debug.LogWarning($"Playing oneshot : {oneShotClipConfig.clip.name} in {oneShotClipConfig.blendInTime}");
                    }
                    EditorGUILayout.LabelField(oneShotClipConfig.clip.name);
                }
            }
        }
    }
}

