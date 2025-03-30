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
            {
                EditorGUILayout.LabelField("Enter playmode to enable playback controls.");
            }

            return;
        }

        var clipHandler = target as ClipHandler;
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("STATES", EditorStyles.boldLabel);
            for (int i = 0; i < clipHandler.clips.Count; i++)
            {
                var clipConfig = clipHandler.clips[i];
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        var effBlendTime = -1f;
                        switch (clipHandler.blendStyle)
                        {
                            case AnimationSystem.ClipBlendStyle.SMOOTHDAMP:
                                // effBlendTime = clipHandler.blendInTime;
                                break;
                            
                            case AnimationSystem.ClipBlendStyle.MOVETOWARDS:
                                // effBlendTime = clipHandler.moveTowardsSpeed;
                                break;
                        }
                        
                        if(clipConfig.overrideBlendInTime)
                            effBlendTime = clipConfig.blendInTime;
                        
                        clipHandler.animSystem.TransitionToState(clipConfig.clip, effBlendTime);
                        
                        Debug.LogWarning($"Transitioning to: {clipConfig.clip.name} in {clipConfig.blendInTime}");
                    }
                    EditorGUILayout.LabelField(clipConfig.clip.name);
                }
            }
        }
    }
}

