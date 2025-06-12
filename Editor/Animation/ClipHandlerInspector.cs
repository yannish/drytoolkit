using System.Collections.Generic;
using drytoolkit.Runtime.Animation;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ClipHandler))]
public class ClipHandlerInspector : Editor
{
    private GUIContent playFromStart;
    private const float buttonWidth = 24f;
    private const float objectFieldWidth = 80f;
    private ClipHandler clipHandler;
    
    private void OnEnable()
    {
        clipHandler = target as ClipHandler;
        
        playFromStart = EditorGUIUtility.IconContent("d_PlayButton");
    }
    
    public override void OnInspectorGUI()
    {
        DrawPlayControls();
        DrawDefaultInspector();
    }

    private int[] layerPicks;
    
    private void DrawPlayControls()
    {
        if (!Application.isPlaying)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.LabelField("Enter playmode to enable playback controls.");
            return;
        }

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("STATE CLIPS:", EditorStyles.boldLabel);
            if (layerPicks == null || layerPicks.Length != clipHandler.stateClips.Count)
            {
                layerPicks = new int[clipHandler.stateClips.Count];
                for (int i = 0; i < clipHandler.stateClips.Count; i++)
                    layerPicks[i] = 0;
            }
            
            for (int i = 0; i < clipHandler.stateClips.Count; i++)
            {
                var clipConfig = clipHandler.stateClips[i];
                
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        // var effectiveBlendTime = clipConfig.overrideBlendInTime
                        //     ? clipConfig.blendInTime
                        //     : clipHandler.blendInTime;
                        
                        clipHandler.animSystem.TransitionToState(clipConfig, layerPicks[i]);
                        //
                        // if(clipHandler.logDebug)
                        //     Debug.LogWarning($"Transitioning to: {clipConfig.clip.name} in {effectiveBlendTime}");
                    }
                    // EditorGUILayout.LabelField(clipConfig.clip.name);
                    // EditorGUILayout.GetControlRect().
                    EditorGUILayout.ObjectField(
                        clipConfig, 
                        typeof(ClipConfig),
                        false, 
                        null
                        );
                    
                    EditorGUI.BeginChangeCheck();
                    var result = EditorGUILayout.IntField(layerPicks[i], GUILayout.Width(objectFieldWidth));
                    if(EditorGUI.EndChangeCheck())
                        layerPicks[i] = result;
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
                        
                        if(clipHandler.reloadEventDef)
                            clipHandler.animSystem.AddListener(clipHandler.reloadEventDef, OnReload);
                        
                        if(clipHandler.midwayEventDef)
                            clipHandler.animSystem.AddListener(clipHandler.midwayEventDef, OnMidway);
                        
                        if(clipHandler.shotFiredEventDef)
                            clipHandler.animSystem.AddListener(clipHandler.shotFiredEventDef, OnShotFired);
                        
                        if(clipHandler.logDebug)
                            Debug.LogWarning($"Playing oneshot : {oneShotClipConfig.clip.name} in {oneShotClipConfig.blendInTime}");
                    }
                    EditorGUILayout.LabelField(oneShotClipConfig.clip.name);
                    EditorGUILayout.ObjectField(oneShotClipConfig, typeof(ClipConfig), false, null);
                }
            }
        }

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("ADDITIVE ONE SHOTS:", EditorStyles.boldLabel);
            for (int i = 0; i < clipHandler.additiveOneShotClips.Count; i++)
            {
                var additiveOneShot = clipHandler.additiveOneShotClips[i];
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        clipHandler.animSystem.PlayAdditiveOneShot(additiveOneShot);
                    }
                    EditorGUILayout.LabelField(additiveOneShot.clip.name);
                    EditorGUILayout.ObjectField(additiveOneShot, typeof(ClipConfig), false, null);
                }
            }
        }

        void OnReload()
        {
            Debug.LogWarning("RELOAD!");
        }

        void OnMidway()
        {
            Debug.LogWarning("MIDWAY!");
        }

        void OnShotFired()
        {
            Debug.LogWarning("SHOTFIRED!");
        }

        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("SEQUENCE CLIPS:", EditorStyles.boldLabel);
            string labelText = "";
            foreach (var clipConfig in clipHandler.sequenceClips)
            {
                labelText += clipConfig.clip.name + " / "; 
                // EditorGUILayout.LabelField(clipConfig.clip.name);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
            {
                Debug.LogWarning("PLAYING SEQUENCE");
                clipHandler.animSystem.PlayOneShot(
                    clipHandler.sequenceClips[0],
                    () => clipHandler.animSystem.PlayOneShot(clipHandler.sequenceClips[1])
                    );
                
                // foreach (var clipConfig in clipHandler.sequenceClips)
                //     clipQueue.Enqueue(clipConfig);
                // PlayClipSequence();
            }
            GUILayout.Label(labelText);
            EditorGUILayout.EndHorizontal();
        }

    }
    
    Queue<ClipConfig> clipQueue = new Queue<ClipConfig>();
    void PlayClipSequence()
    {
        if (clipHandler == null || clipQueue == null || clipQueue.Count == 0)
            return;
            
        var nextClip = clipQueue.Dequeue();
        if(clipQueue.Count > 0)
            clipHandler.animSystem.PlayOneShot(nextClip, () =>
            {
                Debug.LogWarning("next clip in sequence");
                PlayClipSequence();
            });
        else
            clipHandler.animSystem.PlayOneShot(nextClip);
    }

}

