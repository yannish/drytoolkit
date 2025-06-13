using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class AnimationSystem
    {
        public enum ClipBlendStyle
        {
            SMOOTHDAMP,
            MOVETOWARDS
        }

        
        const int MAX_STATE_HANDLES = 16; //TODO:... are these maxes necessary? can't we add / subtract inputs at will?
        const int MAX_ONESHOT_HANDLES = 16;
        public const float BLEND_EPSILON = 0.001f;
        
        public bool logDebug;
        public float currStateSmoothDampTime = -1f;
        public float currStateMoveTowardsTime = -1f;
        public bool rewire = false;
        public bool rebind = false;
        
        
        [Header("BLENDING:")]
        public ClipBlendStyle oneShotBlendStyle = ClipBlendStyle.SMOOTHDAMP;
        public float oneShotSmoothTime = 0.2f;
        public float oneShotMoveToTime = 0.2f;
        
        
        [Header("HANDLES:")]
        public readonly StateClipMixer[] stateClipMixers;
        // [ShowInInspector, DoNotSerialize] List<StateClipHandle> stateClipHandles_PREV => stateClipMixers[0].clipHandles;
        // [ShowInInspector, DoNotSerialize] List<StateClipHandle> stateClipHandles_PREV = new List<StateClipHandle>();
        // [ShowInInspector, DoNotSerialize] public List<List<StateClipHandle>> totalStateClipHandles = new List<List<StateClipHandle>>();
        
        //... this seems like quite a lot of somewhat ill-defined overhead just to smooth out the 
        //... situation where code is calling to play multiple one-shots.
        public List<OneShotClipHandle> oneShotClipHandles = new List<OneShotClipHandle>();
        public List<OneShotClipHandle> additiveOneShotClipHandles = new List<OneShotClipHandle>();

        
        public PlayableGraph graph { get; private set; }
        private Animator animator;
        private Dictionary<ClipEventDefinition, Action> eventLookup = new Dictionary<ClipEventDefinition, Action>();
        
        
        private AnimationClipPlayable oneShotPlayable;
        
        private readonly AnimationLayerMixerPlayable topLevelMixer;

        private readonly AnimationLayerMixerPlayable stateLayerMixer;
        
        private readonly AnimationMixerPlayable oneShotMixer;
        
        private readonly AnimationMixerPlayable additiveOneShotMixer;
        
        public AnimationPlayableOutput playableOutput { get; private set; }

        private MethodInfo rebindMethod;
        
        private bool graphReevaluationQueued = false;
        
        [ReadOnly] public float heaviestOneShot = 0f;


        public AnimationSystem(
            Animator animator, 
            DirectorUpdateMode mode = DirectorUpdateMode.GameTime,
            int layerCount = 1, 
            List<AvatarMask> avatarMasks = null
            )
        {
            this.animator = animator;

            heaviestOneShot = 0f;

            rebindMethod = typeof(Animator).GetMethod(
                "Rebind",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            graph = PlayableGraph.Create(animator.gameObject.name + " - AnimationSystem");

            graph.SetTimeUpdateMode(mode);

            playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);

            //... top level balances between looping states, and one-shots, and additive clips..
            topLevelMixer = AnimationLayerMixerPlayable.Create(graph, 3);

            //... state mixer frantically tries to blend to the most recently requested state:
            stateLayerMixer = AnimationLayerMixerPlayable.Create(graph, layerCount);

            stateClipMixers = new StateClipMixer[layerCount];
            for (int i = 0; i < layerCount; i++)
            {
                var newStateClipMixer = new StateClipMixer
                {
                    mixer = AnimationMixerPlayable.Create(graph, MAX_STATE_HANDLES),
                    clipHandles = new List<StateClipHandle>(),
                };
                stateLayerMixer.ConnectInput(i, newStateClipMixer.mixer, 0);
                stateLayerMixer.SetInputWeight(i, 1f);
                stateClipMixers[i] = newStateClipMixer;
            }

            if (avatarMasks != null)
            {
                for (int i = 0; i < avatarMasks.Count; i++)
                {
                    var avatarMask = avatarMasks[i];
                    if(avatarMask == null)
                        continue;
                    
                    stateLayerMixer.SetLayerMaskFromAvatarMask((uint)i, avatarMask);
                }
            }
            
            // stateMixers = new AnimationMixerPlayable[layerCount];
            // for (int i = 0; i < layerCount; i++)
            // {
            //     stateMixers[i] = AnimationMixerPlayable.Create(graph, MAX_STATE_HANDLES);
            //     stateLayerMixer.ConnectInput(i, stateMixers[i], 0);
            //     stateLayerMixer.SetInputWeight(i, 1f);
            // }
            
            // stateMixer = AnimationMixerPlayable.Create(graph, MAX_STATE_HANDLES);
            
            oneShotMixer = AnimationMixerPlayable.Create(graph, MAX_ONESHOT_HANDLES);
            additiveOneShotMixer = AnimationMixerPlayable.Create(graph, MAX_ONESHOT_HANDLES);

            topLevelMixer.ConnectInput(0, stateLayerMixer, 0);
            // topLevelMixer.ConnectInput(0, stateMixer, 0);
            // stateLayerMixer.ConnectInput(0, stateMixer, 0);
            
            //..
            topLevelMixer.SetInputWeight(0, 1f);
            stateLayerMixer.SetInputWeight(0, 1f);

            topLevelMixer.ConnectInput(1, oneShotMixer, 0);
            topLevelMixer.ConnectInput(2, additiveOneShotMixer, 0);

            topLevelMixer.SetLayerAdditive(2, true);
            
            playableOutput.SetSourcePlayable(topLevelMixer);

            graph.GetRootPlayable(0).SetInputWeight(0, 1f);

            graph.Play();
        }

        public void Tick(ClipBlendStyle blendStyle = ClipBlendStyle.SMOOTHDAMP)
        {
            TickStateBlending(blendStyle);
            
            TickOneShotBlending();

            TickAdditiveOneShotBlending();
            
            topLevelMixer.SetInputWeight(0, 1f - heaviestOneShot);
            topLevelMixer.SetInputWeight(1, 1f);
            topLevelMixer.SetInputWeight(2, 1f);
            
            if(graphReevaluationQueued)
                graph.Evaluate(0f);
            
            // if (logDebug)
            // {
            //     for (int i = 0; i < topLevelMixer.GetInputCount(); i++)
            //     {
            //         Debug.Log($"weight at {i}: {topLevelMixer.GetInputWeight(i)}");
            //     }
            // }
        }

        public void Destroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }


        #region STATE-CLIPS:
        private void TickStateBlending(ClipBlendStyle blendStyle)
        {
            foreach(var stateClipMixer in stateClipMixers)
                stateClipMixer.TickStateBlending(this, blendStyle);
            
            // float totalWeights = 0f;
            // float heaviestWeight = 0f;

            // for (int i = stateClipHandles_PREV.Count - 1; i >= 0; i--)
            // {
            //     var stateClipHandle = stateClipHandles_PREV[i];
            //
            //     switch (blendStyle)
            //     {
            //         case ClipBlendStyle.SMOOTHDAMP:
            //             stateClipHandle.currWeight = Mathf.SmoothDamp(
            //                 stateClipHandle.currWeight,
            //                 stateClipHandle.targetWeight,
            //                 ref stateClipHandle.blendVel,
            //                 currStateSmoothDampTime,
            //                 // stateClipHandle.blendInTime,
            //                 Mathf.Infinity,
            //                 Time.deltaTime
            //             );
            //             break;
            //
            //         case ClipBlendStyle.MOVETOWARDS:
            //             stateClipHandle.currWeight = Mathf.MoveTowards(
            //                 stateClipHandle.currWeight,
            //                 stateClipHandle.targetWeight,
            //                 currStateSmoothDampTime * Time.deltaTime
            //             );
            //             break;
            //     }
            //
            //     if(stateClipHandle.currWeight > heaviestWeight)
            //         heaviestWeight = stateClipHandle.currWeight;
            //     
            //     // Debug.LogWarning($"... blending clip {stateClipHandle.clip.name} in {stateClipHandle.blendInTime}");     
            //     
            //     if (stateClipHandle.targetWeight < 1f && stateClipHandle.currWeight < BLEND_EPSILON)
            //     {
            //         if(logDebug)
            //             Debug.LogWarning($"Removing state clip : {stateClipHandle.clip.name}");
            //         stateClipCountChanged = true;
            //         stateClipHandles_PREV.RemoveAt(i);
            //         graph.DestroyPlayable(stateClipHandle.clipPlayable);
            //     }
            //     else
            //     {
            //         totalWeights += stateClipHandle.currWeight;
            //     }
            // }
            //
            // // var oneOverTotalWeights = 1f / totalWeights;
            // var oneOverTotalWeights = stateClipHandles_PREV.Count == 1 ? 1f : 1f / totalWeights;
            //
            // //... rewire:
            // if (stateClipCountChanged)
            // {
            //     if (rebind)
            //         rebindMethod.Invoke(animator, new object[] { false });
            //
            //     for (int i = 0; i < stateMixer.GetInputCount(); i++)
            //     {
            //         stateMixer.DisconnectInput(i);
            //     }
            //
            //     for (int i = 0; i < stateClipHandles_PREV.Count; i++)
            //     {
            //         stateMixer.ConnectInput(i, stateClipHandles_PREV[i].clipPlayable, 0);
            //     }
            //
            //     // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
            // }
            //
            // for (int i = 0; i < stateClipHandles_PREV.Count; i++)
            // {
            //     // var normalizeWeight = stateClipHandles.Count > 1 ? oneOverTotalWeights : 1f;
            //     var effectiveWeight = stateClipHandles_PREV[i].currWeight * oneOverTotalWeights;// * normalizeWeight;
            //     stateMixer.SetInputWeight(i, effectiveWeight);
            //     // Debug.LogWarning($"effStateWeight: {effectiveWeight}");
            // }
            //
            // // var effectiveStateWeight = stateClipHandles.Count == 1 ? heaviestWeight : 1f;
            // // topLevelMixer.SetInputWeight(0, effectiveStateWeight);
            //             
            // // if(logDebug)
            // //     Debug.LogWarning($"... STATE-WEIGHT: {effectiveStateWeight}");
            //
            // stateClipCountChanged = false;
        }
        
        public void TransitionToState(ClipConfig clipConfig, int layer = 0)
        {
            if (clipConfig == null || clipConfig.clip == null)
            {
                if(clipConfig != null)
                    Debug.LogWarning("... clip config had no clip assigned!", clipConfig);
                else
                    Debug.LogWarning("tried to transition to a null clipconfig.");
                return;
            }

            currStateSmoothDampTime = clipConfig.smoothDampBlendTime;
            currStateMoveTowardsTime = clipConfig.moveTowardsBlendTime;

            TransitionToState(
                clipConfig.clip,
                clipConfig.blendInTime,
                clipConfig.startTime,
                clipConfig.playbackSpeed, 
                layer
            );
        }

        public void TransitionToState(
            AnimationClip newStateClip,
            float blendInTime = 0f,
            float startTime = 0f,
            float playbackSpeed = 1f,
            int layer = 0
        )
        {
            if (newStateClip == null)
            {
                Debug.LogWarning("... tried to transition to clip, but it was null.");
                return;
            }
            
            //... search to see if this clip is already among those being blended between.
            bool stateClipAlreadyExists = false;
            
            for (int i = 0; i < stateClipMixers[layer].clipHandles.Count; i++)
            {
                var stateClipHandle = stateClipMixers[layer].clipHandles[i];
                var stateClip = stateClipHandle.clip;
                
                //... if found, we just set its target weight back to 1f.
                if (stateClip == newStateClip)
                {
                    stateClipAlreadyExists = true;
                    stateClipHandle.blendInOverrideTime = blendInTime;
                    stateClipHandle.targetWeight = 1f;
                }
                else
                {
                    stateClipHandle.targetWeight = 0f;
                }
            }

            //... otherwise we add it and link it within the graph:
            if (!stateClipAlreadyExists)
            {
                stateClipMixers[layer].stateClipCountChanged = true;
                
                var newStateClipHandle = new StateClipHandle()
                {
                    clip = newStateClip,
                    clipPlayable = AnimationClipPlayable.Create(graph, newStateClip),
                    blendInOverrideTime = blendInTime,
                    targetWeight = 1f
                };
                
                newStateClipHandle.clipPlayable.SetTime(startTime);
                newStateClipHandle.clipPlayable.SetSpeed(playbackSpeed);
                
                stateClipMixers[layer].clipHandles.Add(newStateClipHandle);

                graphReevaluationQueued = true;
            }
        }

        #endregion

        #region ONE-SHOTS:
        private void TickOneShotBlending()
        {
            bool oneShotClipCountChanged = false;
            
            float leadingClipWeight = 0f;
            float accumulatedWeight = 0f;
            heaviestOneShot = 0f;
            
            if (oneShotClipHandles.Count <= 0)
                return;
            
            for (int i = oneShotClipHandles.Count - 1; i >= 0; i--)
            {
                var clipHandle = oneShotClipHandles[i];
                var prevWeight = clipHandle.currWeight;
                
                //... this is the local weight of the clip according to ITS params :
                var currWeight = (float)clipHandle.GetCurrentBlendWeight();
                // clipHandle.currWeight = currWeight;
                // clipHandle.currWeight = (float)clipHandle.GetCurrentBlendWeight();

                if(currWeight > heaviestOneShot)
                    heaviestOneShot = currWeight;
                
                bool leadingClip = i == oneShotClipHandles.Count - 1;
                if (leadingClip)
                {
                    leadingClipWeight = clipHandle.currWeight;
                    clipHandle.currWeight = currWeight;
                }
                else
                {
                    clipHandle.currWeight = Mathf.Min(currWeight, 1f - leadingClipWeight);
                }
                
                if (prevWeight < 1f && clipHandle.currWeight >= 1f)
                {
                    if(logDebug)
                        Debug.LogWarning($"Done blending oneshot in, time: {clipHandle.clipPlayable.GetTime()}");
                }

                if (prevWeight > 0f && clipHandle.currWeight <= 0f)
                {
                    if(logDebug)
                        Debug.LogWarning($"Done blending oneshot out, time: {clipHandle.clipPlayable.GetTime()}");
                }

                //... handle events:
                if (clipHandle.config != null)
                {
                    var prevTime = clipHandle.clipPlayable.GetPreviousTime();
                    var currTime = clipHandle.clipPlayable.GetTime();
                    foreach(var clipEvent in clipHandle.config.events)
                    {
                        if(clipEvent.clipEventDefinition == null)
                            continue;
                        
                        if (clipEvent.time > prevTime && clipEvent.time <= currTime)
                        {
                            Debug.LogWarning($"Firing event: {clipEvent.clipEventDefinition}");
                            if (eventLookup.TryGetValue(clipEvent.clipEventDefinition, out var callback))
                            {
                                callback?.Invoke();
                                //... TODO: remove here... this seems wrong though.
                                //... what if you had multiple events on a clip tied to the same definition...
                                //... well then ADDING them to begin with would be breaking...
                                //... that would mean duplicate entries in the dictionary...
                                // eventLookup.Remove(clipEvent.clipEventDefinition);
                            }
                        }
                    }
                }

                bool clipIsComplete = false;
                if (clipHandle.clipPlayable.GetTime() >= clipHandle.clipPlayable.GetDuration()) 
                {
                    switch (clipHandle.wrapMode)
                    {
                        case WrapMode.Once:
                            clipIsComplete = true;
                            clipHandle.onCompleteCallback?.Invoke();
                            if(logDebug)
                                Debug.LogWarning($"Clip ran its time: {clipHandle.clipPlayable.GetTime()}");
                            break;
                        
                        case WrapMode.Loop:
                            clipHandle.clipPlayable.SetTime(clipHandle.clipPlayable.GetTime() % 1f);
                            if(logDebug)
                                Debug.LogWarning("Looping clip.");
                            break;
                        
                        case WrapMode.PingPong:
                            break;
                        
                        case WrapMode.Default:
                            break;
                        
                        case WrapMode.ClampForever:
                            break;
                    }
                    
                    //... TODO: clean events here?
                    //... don't do it if we're NOT the leading clip. otherwise non-leading clips coming to completion 
                    //... would remove callbacks we want...
                    if(leadingClip)
                        ClearEventListeners();
                }
                
                //... if isn't the leading one and weight has been driven down, it's done:
                if (!leadingClip && clipHandle.currWeight <= 0f)
                {
                    if(logDebug)
                        Debug.LogWarning("clip done by blending weight down.");
                    
                    clipIsComplete = true;
                }
                
                if (clipIsComplete)
                {
                    if(logDebug)
                        Debug.LogWarning($"DONE WITH ONESHOT : {clipHandle.clip.name}");
                    
                    if (clipHandle.clipPlayable.IsValid())
                    {
                        oneShotClipHandles.RemoveAt(i);
                        graph.DestroyPlayable(clipHandle.clipPlayable);
                    }

                    if (i == oneShotClipHandles.Count - 1)
                    {
                        //... if we're done with the leading clip, flush out all the rest as well.
                    }
                    
                    oneShotClipCountChanged = true;
                }

                accumulatedWeight += clipHandle.currWeight;
            }
            
            //... re-wire our playables if any clips have been discarded:
            if (oneShotClipCountChanged)
            {
                // if (rebind)
                // rebindMethod.Invoke(animator, new object[] { false });
                // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
                
                for (int i = 0; i < oneShotMixer.GetInputCount(); i++)
                    oneShotMixer.DisconnectInput(i);
                
                for (int i = 0; i < oneShotClipHandles.Count; i++)
                    oneShotMixer.ConnectInput(i, oneShotClipHandles[i].clipPlayable, 0);
                
                if(rebind)
                    playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
            }

            for (int i = 0; i < oneShotClipHandles.Count; i++)
            {
                oneShotMixer.SetInputWeight(i, oneShotClipHandles[i].currWeight);
            }
            
            accumulatedWeight = Mathf.Clamp01(accumulatedWeight);
            
            // topLevelMixer.SetInputWeight(0, 1f - accumulatedWeight);
            // topLevelMixer.SetInputWeight(1, accumulatedWeight);
        }

        public OneShotClipHandle PlayOneShot(ClipConfig clipConfig, Action callback = null)
        {
            var newOneShotClipHandle = PlayOneShot(
                clipConfig.clip,
                clipConfig.blendInTime,
                clipConfig.blendOutTime,
                clipConfig.startTime,
                clipConfig.playbackSpeed,
                clipConfig.wrapMode,
                callback
            );

            if(newOneShotClipHandle == null)
                return null;
            
            newOneShotClipHandle.config = clipConfig;

            return newOneShotClipHandle;
        }

        public OneShotClipHandle PlayOneShot(
            AnimationClip clip,
            float blendInTime = 0f,
            float blendOutTime = 0f,
            float startTime = 0f,
            float playbackSpeed = 1f,
            WrapMode wrapMode = WrapMode.Once,
            Action callback = null
            )
        {
            //... if this interrupts a prev oneshot, it doesn't get to completion to clear listeners there, so do it now...
            ClearEventListeners();
            
            if (oneShotClipHandles.Count > 0 && oneShotClipHandles[0].clip == clip)
            {
                Debug.LogWarning($"Already blending to one-shot clip : {clip.name}", clip);
                return null;
            }

            //... otherwise, check that we can add a new oneShotHandle
            
            //... and then add it:
            var newOneShotClipHandle = new OneShotClipHandle()
            {
                clip = clip,
                //... if this is first oneShot added to our stack, we start at a weight of 1f.
                //... blending still happens with the topLevel mixer.
                blendInTime = blendInTime,
                // blendInTime = oneShotClipHandles.Count == 0 ? 0f : blendInTime,
                blendOutTime = blendOutTime,
                clipPlayable = AnimationClipPlayable.Create(graph, clip)
            };
            
            // if(oneShotClipHandles.Count)
            
            if(callback != null)
                newOneShotClipHandle.onCompleteCallback += callback;

            // newOneShotClipHandle.callbackTime = clip.length * 0.5f;
            // newOneShotClipHandle.callBack += () => Debug.LogWarning("Halfway callback!");
            
            newOneShotClipHandle.wrapMode = wrapMode;
            newOneShotClipHandle.clipPlayable.SetTime(startTime);
            newOneShotClipHandle.clipPlayable.SetSpeed(playbackSpeed);
            newOneShotClipHandle.clipPlayable.SetDuration(clip.length / playbackSpeed);
            newOneShotClipHandle.clipPlayable.Play();

            oneShotMixer.ConnectInput(oneShotClipHandles.Count, newOneShotClipHandle.clipPlayable, 0);
            oneShotMixer.SetInputWeight(oneShotClipHandles.Count, 0f);
            
            oneShotClipHandles.Add(newOneShotClipHandle);
            
            if(rebind)
                playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());

            return newOneShotClipHandle;
        }
        
        void InterruptOneShot()
        {
            topLevelMixer.SetInputWeight(0, 1f);
            topLevelMixer.SetInputWeight(1, 0f);

            if (oneShotPlayable.IsValid())
                DisconnectOneShot();
        }

        void DisconnectOneShot()
        {
            topLevelMixer.DisconnectInput(1);
            graph.DestroyPlayable(oneShotPlayable);
        }
        #endregion

        #region ADDITIVE ONE-SHOTS:
        private void TickAdditiveOneShotBlending()
        {
            if (additiveOneShotClipHandles.Count <= 0)
                return;

            var clipCountChanged = false;
            
            for (int i =  additiveOneShotClipHandles.Count - 1; i >= 0; i--)
            {
                var clipHandle = additiveOneShotClipHandles[i];
                clipHandle.currWeight = (float)clipHandle.GetCurrentBlendWeight();
                
                Debug.LogWarning($"weight: {clipHandle.currWeight}");

                var clipComplete = false;
                var clipTime = clipHandle.clipPlayable.GetTime();
                //... TODO: is GetTime() isn't actually normalized...? where else are we treating it like it is...?
                if (
                    clipHandle.clipPlayable.IsDone()
                    // && clipHandle.clipPlayable.GetTime() >= 1f
                    )
                {
                    switch (clipHandle.wrapMode)
                    {
                        case WrapMode.Once:
                            clipComplete = true;
                            clipHandle.onCompleteCallback?.Invoke();
                            if(logDebug)
                                Debug.LogWarning("Additive clip ran its time.");
                            break;
                        
                        case WrapMode.Loop:
                            clipHandle.clipPlayable.SetTime(clipHandle.clipPlayable.GetTime() % 1f);
                            if(logDebug)
                                Debug.LogWarning("Looping additive clip.");
                            break;
                        
                        case WrapMode.PingPong:
                            break;
                        
                        case WrapMode.Default:
                            break;
                        
                        case WrapMode.ClampForever:
                            break;
                    }
                }

                if (clipComplete)
                {
                    if (clipHandle.clipPlayable.IsValid())
                    {
                        additiveOneShotClipHandles.RemoveAt(i);
                        graph.DestroyPlayable(clipHandle.clipPlayable);
                    }

                    clipCountChanged = true;
                }
            }
            
            if (clipCountChanged)
            {
                for (int i = 0; i < additiveOneShotMixer.GetInputCount(); i++)
                    additiveOneShotMixer.DisconnectInput(i);
                
                for (int i = 0; i < additiveOneShotClipHandles.Count; i++)
                    additiveOneShotMixer.ConnectInput(i, additiveOneShotClipHandles[i].clipPlayable, 0);
            }

            for (int i = 0; i < additiveOneShotClipHandles.Count; i++)
            {
                additiveOneShotMixer.SetInputWeight(i, additiveOneShotClipHandles[i].currWeight);
            }
        }

        public OneShotClipHandle PlayAdditiveOneShot(ClipConfig clipConfig, Action callback = null)
        {
            var newAdditiveOneShotClipHandle = PlayAdditiveOneShot(
                clipConfig.clip,
                clipConfig.blendInTime,
                clipConfig.blendOutTime,
                clipConfig.startTime,
                clipConfig.playbackSpeed,
                clipConfig.wrapMode,
                callback
            );
            
            newAdditiveOneShotClipHandle.config = clipConfig;
            
            return newAdditiveOneShotClipHandle;
        }

        public OneShotClipHandle PlayAdditiveOneShot(
            AnimationClip clip,
            float blendInTime,
            float blendOutTime,
            float startTime = 0f,
            float playbackSpeed = 1f,
            WrapMode wrapMode = WrapMode.Once,
            Action callback = null
            )
        {
            // if (additiveOneShotClipHandles.Select(t => t.clip == clip) != null)
            // {
            //     Debug.LogWarning($"Already playing additive one-shot clip : {clip.name}", clip);
            //     return null;
            // }

            var newAdditiveOneShotClip = new OneShotClipHandle()
            {
                clip = clip,
                blendInTime = blendInTime,
                blendOutTime = blendOutTime,
                targetWeight = 1f,
                clipPlayable = AnimationClipPlayable.Create(graph, clip),
                currWeight = blendInTime > 0f ? 0f : 1f
            };

            if(callback != null)
                newAdditiveOneShotClip.onCompleteCallback += callback;
            
            newAdditiveOneShotClip.wrapMode = wrapMode;
            newAdditiveOneShotClip.clipPlayable.SetTime(startTime);
            newAdditiveOneShotClip.clipPlayable.SetSpeed(playbackSpeed);
            newAdditiveOneShotClip.clipPlayable.SetDuration(clip.length / playbackSpeed);
            newAdditiveOneShotClip.clipPlayable.Play();
            
            additiveOneShotMixer.ConnectInput(additiveOneShotClipHandles.Count, newAdditiveOneShotClip.clipPlayable, 0);
            additiveOneShotMixer.SetInputWeight(additiveOneShotClipHandles.Count, newAdditiveOneShotClip.currWeight);
            // additiveOneShotMixer.SetInputWeight(additiveOneShotClipHandles.Count, 0f);
            
            additiveOneShotClipHandles.Add(newAdditiveOneShotClip);

            return newAdditiveOneShotClip;
        }
        
        #endregion

        #region EVENTS:
        public void AddListener(ClipEventDefinition clipEventDefinition, Action callback)
        {
            if (clipEventDefinition == null)
            {
                Debug.LogWarning("Tried to add a listener without a clip definition.");
                return;
            }
            
            if (eventLookup.TryGetValue(clipEventDefinition, out var callbacks))
            {
                callbacks += callback;
                return;
            }

            eventLookup.Add(clipEventDefinition, callback);
        }

        public void RemoveListener(ClipEventDefinition clipEventDefinition)
        {
            if(eventLookup.ContainsKey(clipEventDefinition))
                eventLookup.Remove(clipEventDefinition);
        }

        public void ClearEventListeners() => eventLookup.Clear();
        #endregion
        
        #region SEQUENCES:
        Queue<ClipConfig> clipQueue = new Queue<ClipConfig>();
        public void PlaySequence(List<ClipConfig> clipConfigs)
        {
            clipQueue.Clear();
            foreach(var clipConfig in clipConfigs)
                clipQueue.Enqueue(clipConfig);
            
            var clipToPlayFirst = clipQueue.Dequeue();
            InterruptOneShot();
            PlayOneShot(clipToPlayFirst);
        }
        #endregion
        
        // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
        // rebindMethod.Invoke(animator, new object[] { false });
        
        // if (rewire)
        //     playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
    }
}

