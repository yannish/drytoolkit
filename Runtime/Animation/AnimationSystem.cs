using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine.Animations;
using UnityEngine.Playables;


namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class AnimationSystem
    {
        const int MAX_STATE_HANDLES = 16;
        const int MAX_ONESHOT_HANDLES = 16;
        const float BLEND_EPSILON = 0.001f;
        
        public enum ClipBlendStyle
        {
            SMOOTHDAMP,
            MOVETOWARDS
        }


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
        public List<StateClipHandle> stateClipHandles = new List<StateClipHandle>();
        //... this seems like quite a lot of somewhat ill-defined overhead just to smooth out the 
        //... situation where code is calling to play multiple one-shots.
        public List<OneShotClipHandle> oneShotClipHandles = new List<OneShotClipHandle>();
        public List<OneShotClipHandle> additiveOneShotClipHandles = new List<OneShotClipHandle>();

        
        public PlayableGraph graph { get; private set; }
        
        Dictionary<ClipEventDefinition, Action> eventLookup = new Dictionary<ClipEventDefinition, Action>();
        // Dictionary<ClipEventDefinition, List<Action>> eventLookup = new Dictionary<ClipEventDefinition, List<Action>>();
        
        private Animator animator;
        
        private AnimationClipPlayable oneShotPlayable;
        private readonly AnimationLayerMixerPlayable topLevelMixer;
        private readonly AnimationMixerPlayable stateMixer;
        private readonly AnimationMixerPlayable oneShotMixer;
        private readonly AnimationMixerPlayable oneShotAdditiveMixer;
        private readonly AnimationPlayableOutput playableOutput;

        private MethodInfo rebindMethod;
        
        bool stateClipCountChanged = false;

        
        public AnimationSystem(Animator animator, DirectorUpdateMode mode = DirectorUpdateMode.GameTime)
        {
            this.animator = animator;

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
            stateMixer = AnimationMixerPlayable.Create(graph, MAX_STATE_HANDLES);
            oneShotMixer = AnimationMixerPlayable.Create(graph, MAX_ONESHOT_HANDLES);
            oneShotAdditiveMixer = AnimationMixerPlayable.Create(graph, MAX_ONESHOT_HANDLES);

            topLevelMixer.ConnectInput(0, stateMixer, 0);
            topLevelMixer.ConnectInput(1, oneShotMixer, 0);
            topLevelMixer.ConnectInput(2, oneShotAdditiveMixer, 0);

            playableOutput.SetSourcePlayable(topLevelMixer);

            graph.GetRootPlayable(0).SetInputWeight(0, 1f);

            graph.Play();
        }

        public void Tick(ClipBlendStyle blendStyle = ClipBlendStyle.SMOOTHDAMP)
        {
            TickStateBlending(blendStyle);
            TickOneShotBlending();
            // TickOneShotBlending();
        }


        #region STATE-CLIPS:

        //... STATE:
        private void TickStateBlending(ClipBlendStyle blendStyle)
        {
            float totalWeights = 0f;
            float heaviestWeight = 0f;
            for (int i = stateClipHandles.Count - 1; i >= 0; i--)
            {
                var stateClipHandle = stateClipHandles[i];
                // var stateClip = stateClipHandle.clip;

                // if ((stateClipHandle.currWeight - stateClipHandle.targetWeight) > BLEND_EPSILON)
                // {
                switch (blendStyle)
                {
                    case ClipBlendStyle.SMOOTHDAMP:
                        stateClipHandle.currWeight = Mathf.SmoothDamp(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            ref stateClipHandle.blendVel,
                            currStateSmoothDampTime,
                            // stateClipHandle.blendInTime,
                            Mathf.Infinity,
                            Time.deltaTime
                        );
                        break;

                    case ClipBlendStyle.MOVETOWARDS:
                        stateClipHandle.currWeight = Mathf.MoveTowards(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            currStateSmoothDampTime * Time.deltaTime
                        );
                        break;
                }
                // }

                if(stateClipHandle.currWeight > heaviestWeight)
                    heaviestWeight = stateClipHandle.currWeight;
                
                // Debug.LogWarning($"... blending clip {stateClipHandle.clip.name} in {stateClipHandle.blendInTime}");            
                if (stateClipHandle.targetWeight < 1f && stateClipHandle.currWeight < BLEND_EPSILON)
                {
                    if(logDebug)
                        Debug.LogWarning($"Removing state clip : {stateClipHandle.clip.name}");
                    stateClipCountChanged = true;
                    stateClipHandles.RemoveAt(i);
                    graph.DestroyPlayable(stateClipHandle.clipPlayable);
                }
                else
                {
                    totalWeights += stateClipHandle.currWeight;
                }
            }

            var oneOverTotalWeights = 1f / totalWeights;
            // var oneOverTotalWeights = stateClipHandles.Count == 1 ? 1f : 1f / totalWeights;

            //... rewire:
            if (stateClipCountChanged)
            {
                if (rebind)
                    rebindMethod.Invoke(animator, new object[] { false });

                for (int i = 0; i < stateMixer.GetInputCount(); i++)
                {
                    stateMixer.DisconnectInput(i);
                }

                for (int i = 0; i < stateClipHandles.Count; i++)
                {
                    stateMixer.ConnectInput(i, stateClipHandles[i].clipPlayable, 0);
                }

                // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
            }

            for (int i = 0; i < stateClipHandles.Count; i++)
            {
                stateMixer.SetInputWeight(i, stateClipHandles[i].currWeight * oneOverTotalWeights);
            }
            
            topLevelMixer.SetInputWeight(0, stateClipHandles.Count == 1 ? heaviestWeight : 1f);

            stateClipCountChanged = false;
        }
        
        public void TransitionToState(ClipConfig clipConfig)
        {
            if (clipConfig.clip == null)
            {
                Debug.LogWarning("... clip config had no clip assigned!", clipConfig);
                return;
            }

            currStateSmoothDampTime = clipConfig.smoothDampBlendTime;
            currStateMoveTowardsTime = clipConfig.moveTowardsBlendTime;

            TransitionToState(
                clipConfig.clip,
                clipConfig.blendInTime,
                clipConfig.startTime,
                clipConfig.playbackSpeed
            );
        }

        public void TransitionToState(
            AnimationClip newStateClip,
            float blendInTime = 0f,
            float startTime = 0f,
            float playbackSpeed = 1f
        )
        {
            //... search to see if this clip is already among those being blended between.
            bool stateClipAlreadyExists = false;
            for (int i = 0; i < stateClipHandles.Count; i++)
            {
                var stateClipHandle = stateClipHandles[i];
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
                stateClipCountChanged = true;
                var newStateClipHandle = new StateClipHandle()
                {
                    clip = newStateClip,
                    clipPlayable = AnimationClipPlayable.Create(graph, newStateClip),
                    blendInOverrideTime = blendInTime,
                    targetWeight = 1f
                };
                newStateClipHandle.clipPlayable.SetTime(startTime);
                newStateClipHandle.clipPlayable.SetSpeed(playbackSpeed);
                stateClipHandles.Add(newStateClipHandle);
            }
        }

        #endregion

        #region ONE-SHOTS:

        //... ONE SHOTS:
        private void TickOneShotBlending()
        {
            if (oneShotClipHandles.Count <= 0)
                return;
            
            bool oneShotClipCountChanged = false;
            
            float leadingClipWeight = 0f;
            float accumulatedWeight = 0f;
            
            for (int i = oneShotClipHandles.Count - 1; i >= 0; i--)
            {
                var clipHandle = oneShotClipHandles[i];
                var prevWeight = clipHandle.currWeight;
                var currWeight = (float)clipHandle.GetCurrentBlendWeight();
                
                clipHandle.currWeight = (float)clipHandle.GetCurrentBlendWeight();

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
                if (clipHandle.clipPlayable.GetTime() >= 1f)
                {
                    switch (clipHandle.wrapMode)
                    {
                        case WrapMode.Once:
                            clipIsComplete = true;
                            clipHandle.onCompleteCallback?.Invoke();
                            if(logDebug)
                                Debug.LogWarning("Clip ran its time.");
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
                    ClearEventListeners();
                }

                //... if isn't the leading one and weight has been driven down, it's done:
                if (!leadingClip && clipHandle.currWeight <= 0f)
                    clipIsComplete = true;
                
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
            
            if (oneShotClipCountChanged)
            {
                for (int i = 0; i < oneShotMixer.GetInputCount(); i++)
                    oneShotMixer.DisconnectInput(i);
                
                for (int i = 0; i < oneShotClipHandles.Count; i++)
                    oneShotMixer.ConnectInput(i, oneShotClipHandles[i].clipPlayable, 0);
            }

            for (int i = 0; i < oneShotClipHandles.Count; i++)
            {
                oneShotMixer.SetInputWeight(i, oneShotClipHandles[i].currWeight);
            }
            
            accumulatedWeight = Mathf.Clamp01(accumulatedWeight);
            
            topLevelMixer.SetInputWeight(0, 1f - accumulatedWeight);
            topLevelMixer.SetInputWeight(1, accumulatedWeight);
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

            newOneShotClipHandle.config = clipConfig;

            return newOneShotClipHandle;
        }

        public OneShotClipHandle PlayOneShot(
            AnimationClip clip,
            float blendInTime,
            float blendOutTime,
            float startTime = 0f,
            float playbackSpeed = 1f,
            WrapMode wrapMode = WrapMode.Once,
            Action callback = null
            )
        {
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
                blendInTime = blendInTime,
                blendOutTime = blendOutTime,
                clipPlayable = AnimationClipPlayable.Create(graph, clip)
            };
            
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

        

        #endregion

        #region EVENTS:
        public void AddListener(ClipEventDefinition clipEventDefinition, Action callback)
        {
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

        
        //... SEQUENCES:
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
        
        public void Destroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }
        

        // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
        // rebindMethod.Invoke(animator, new object[] { false });
        
        // if (rewire)
        //     playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
    }
}

