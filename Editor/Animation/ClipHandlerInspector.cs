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

    private int[] layerClipPicks;
    private int[] layerClipConfigPicks;
    
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
            if (layerClipPicks == null || layerClipPicks.Length != clipHandler.stateClips.Count)
            {
                layerClipPicks = new int[clipHandler.stateClips.Count];
                for (int i = 0; i < clipHandler.stateClips.Count; i++)
                    layerClipPicks[i] = 0;
            }
            
            for (int i = 0; i < clipHandler.stateClips.Count; i++)
            {
                var stateClip = clipHandler.stateClips[i];
                
                using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                    {
                        clipHandler.animSystem.TransitionToState(stateClip, clipHandler.blendInTime, layer: layerClipPicks[i]);
                    }
                    
                    EditorGUILayout.ObjectField(
                        stateClip, 
                        typeof(ClipConfig),
                        false, 
                        null
                        );
                    
                    EditorGUI.BeginChangeCheck();
                    var result = EditorGUILayout.IntField(layerClipPicks[i], GUILayout.Width(objectFieldWidth));
                    if(EditorGUI.EndChangeCheck())
                        layerClipPicks[i] = result;
                }
            }
        }

        if (clipHandler.clipHandlerConfigs.Count > 0)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("CLIP HANDLER-CLIPS:", EditorStyles.boldLabel);
                for (int i = 0; i < clipHandler.clipHandlerConfigs.Count; i++)
                {
                    var clipConfig = clipHandler.clipHandlerConfigs[i];
                    
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                        {
                            clipHandler.animSystem.TransitionToState(
                                clipConfig.clip,
                                blendInTime: clipConfig.smoothDampTime,
                                layer: clipConfig.layer,
                                blendStyle: clipConfig.blendInStyle
                                );

                            // if(clipHandler.logDebug)
                            Debug.LogWarning($"Transitioning to: {clipConfig.clip.name} in {clipHandler.blendInTime}");
                        }
                        
                        // EditorGUILayout.LabelField(clipConfig.clip.name);
                        // EditorGUILayout.GetControlRect().
                        
                        EditorGUILayout.ObjectField(
                            clipConfig.clip, 
                            typeof(ClipConfig),
                            false, 
                            null
                            );
                        
                        // EditorGUI.BeginChangeCheck();
                        // EditorGUILayout.FloatField(clipConfig.blendInTime, GUILayout.Width(objectFieldWidth));
                        // if(EditorGUI.EndChangeCheck())
                        //     clipConfig.blendInTime = 
                        //     
                        // EditorGUI.BeginChangeCheck();
                        // var result = EditorGUILayout.IntField(layerClipPicks[i], GUILayout.Width(objectFieldWidth));
                        // if(EditorGUI.EndChangeCheck())
                        //     layerClipPicks[i] = result;
                    }
                }
            }
        }

        if (clipHandler.stateClipConfigs.Count > 0)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("STATE CONFIG-CLIPS:", EditorStyles.boldLabel);
                if (layerClipConfigPicks == null || layerClipConfigPicks.Length != clipHandler.stateClipConfigs.Count)
                {
                    layerClipConfigPicks = new int[clipHandler.stateClipConfigs.Count];
                    for (int i = 0; i < clipHandler.stateClipConfigs.Count; i++)
                        layerClipConfigPicks[i] = 0;
                }
                
                for (int i = 0; i < clipHandler.stateClipConfigs.Count; i++)
                {
                    var clipConfig = clipHandler.stateClipConfigs[i];
                    
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                        {
                            // var effectiveBlendTime = clipConfig.overrideBlendInTime
                            //     ? clipConfig.blendInTime
                            //     : clipHandler.blendInTime;
                            
                            clipHandler.animSystem.TransitionToState(clipConfig, layerClipPicks[i]);

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
                        var result = EditorGUILayout.IntField(layerClipPicks[i], GUILayout.Width(objectFieldWidth));
                        if(EditorGUI.EndChangeCheck())
                            layerClipPicks[i] = result;
                    }
                }
            }
        }
        
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (clipHandler.oneShotClips.Count > 0)
            {
                EditorGUILayout.LabelField("ONE SHOT CLIPS:", EditorStyles.boldLabel);
                for (int i = 0; i < clipHandler.oneShotClips.Count; i++)
                {
                    var oneShotClip = clipHandler.oneShotClips[i];
                    if (oneShotClip == null)
                        continue;
                    
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                        {
                            // var effectiveBlendTime = clipConfig.overrideBlendInTime
                            //     ? clipConfig.blendInTime
                            //     : clipHandler.blendInTime;
                            
                            clipHandler.animSystem.PlayOneShot(
                                oneShotClip,
                                clipHandler.oneShotBlendIn,
                                clipHandler.oneShotBlendOut
                                );
                            
                            if(clipHandler.reloadEventDef)
                                clipHandler.animSystem.AddListener(clipHandler.reloadEventDef, OnReload);
                            
                            if(clipHandler.midwayEventDef)
                                clipHandler.animSystem.AddListener(clipHandler.midwayEventDef, OnMidway);
                            
                            if(clipHandler.shotFiredEventDef)
                                clipHandler.animSystem.AddListener(clipHandler.shotFiredEventDef, OnShotFired);
                            
                            if(clipHandler.logDebug)
                                Debug.LogWarning($"Playing oneshot : {oneShotClip.name} in {clipHandler.oneShotBlendIn}");
                        }
                        EditorGUILayout.LabelField(oneShotClip.name);
                        EditorGUILayout.ObjectField(oneShotClip, typeof(AnimationClip), false, null);
                    }
                }
            }
            
            if (clipHandler.oneShotClipConfigs.Count > 0)
            {
                EditorGUILayout.LabelField("ONE SHOT CLIP CONFIGS:", EditorStyles.boldLabel);
                for (int i = 0; i < clipHandler.oneShotClipConfigs.Count; i++)
                {
                    var oneShotClipConfig = clipHandler.oneShotClipConfigs[i];
                    if (oneShotClipConfig == null)
                        continue;
                    
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
        }
        
        if (clipHandler.additiveOneShotClips.Count > 0)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("ADDITIVE ONE SHOTS:", EditorStyles.boldLabel);
                for (int i = 0; i < clipHandler.additiveOneShotClips.Count; i++)
                {
                    var additiveOneShot = clipHandler.additiveOneShotClips[i];
                    using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button(playFromStart, GUILayout.Width(buttonWidth)))
                            clipHandler.animSystem.PlayAdditiveOneShot(additiveOneShot);
                        EditorGUILayout.LabelField(additiveOneShot.name);
                        EditorGUILayout.ObjectField(additiveOneShot, typeof(ClipConfig), false, null);
                    }
                }
            }
        }

        if (clipHandler.additiveOneShotClipConfigs.Count > 0)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("ADDITIVE ONE SHOTS:", EditorStyles.boldLabel);
                for (int i = 0; i < clipHandler.additiveOneShotClipConfigs.Count; i++)
                {
                    var additiveOneShot = clipHandler.additiveOneShotClipConfigs[i];
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

