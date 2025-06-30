using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimpleClipPlayer))]
public class SimpleClipPlayerInspector : Editor
{
    private GUIContent playFromStart;
    private const float buttonWidth = 24f;

    public override void OnInspectorGUI()
    {
        DrawControls();
        DrawDefaultInspector();
    }

    private void OnEnable()
    {
        playFromStart = EditorGUIUtility.IconContent("d_PlayButton");
    }

    private void DrawControls()
    {
        if (!Application.isPlaying)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.LabelField("Enter playmode to enable playback controls.");
            return;
        }
        
        var simpleClipPlayer = (SimpleClipPlayer)target;
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("CLIPS:", EditorStyles.boldLabel);
            
            for (int i = 0; i < simpleClipPlayer.layeredClips.Count; i++)
            {
                var layeredClip = simpleClipPlayer.layeredClips[i];
                if (layeredClip.clip == null)
                    continue;
                
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        simpleClipPlayer.simpleAnimationSystem.TransitionToClip(layeredClip.clip, layeredClip.layer);
                    }
                    
                    EditorGUILayout.ObjectField(
                        layeredClip.clip, 
                        typeof(AnimationClip),
                        false, 
                        null
                    );
                }
            }
        }
    }
}
