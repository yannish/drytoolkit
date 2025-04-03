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
        public enum ClipBlendStyle
        {
            SMOOTHDAMP,
            MOVETOWARDS
        }

        [Serializable]
        public class ClipHandle
        {
            public AnimationClip clip;
            public AnimationClipPlayable clipPlayable;

            //... state stuff:
            public float blendVel = 0f;
            public float targetWeight = 0f;
            public float currWeight = 0f;
            
            public void SmoothDampToTarget(float smoothTime)
            {
                currWeight = Mathf.SmoothDamp(
                    currWeight, targetWeight, ref blendVel, smoothTime
                );
            }

            public void MoveTowardsTargetWeight(float moveSpeed)
            {
                currWeight = Mathf.MoveTowards(currWeight, targetWeight, moveSpeed * Time.deltaTime);
            }
        }
        
        [Serializable]
        public class OneShotClipHandle : ClipHandle
        {
            public bool additive;
            public float blendInTime = 0.1f;
            public float blendOutTime = 0.1f;

            public float callbackTime;
            public Action callBack;
            
            public double GetCurrentBlendWeight()
            {
                double currClipTime = clipPlayable.GetTime();
                
                var effLength = clipPlayable.GetDuration();

                var effBlendInTime = blendInTime * clipPlayable.GetSpeed();
                
                if (effBlendInTime > 0f && currClipTime <= effBlendInTime)
                    return (float)(currClipTime / effBlendInTime);
                
                var effBlendOutTime = blendOutTime * clipPlayable.GetSpeed();

                // Debug.LogWarning($"currClipTime : {currClipTime}, effectiveLength : {effLength - effBlendOutTime}");

                if (blendOutTime > 0f && currClipTime >= (1f - effBlendOutTime))
                {
                    var result = 1f - math.clamp(
                        (currClipTime - (1f - effBlendOutTime)) / effBlendOutTime,
                        0,
                        1
                    );
                    return result;
                }

                
                return 1f;
            }

            public float GetEffectiveLength()
            {
                float speed = (float)clipPlayable.GetSpeed();
                return speed != 0 ? (float)clipPlayable.GetDuration() / speed : Mathf.Infinity;
            }
        }

        [Serializable]
        public class StateClipHandle : ClipHandle//... TODO: return a reference to this when it's created... & do stuff.
        {
            /*
             * What stuff...
             * ... cancel the anim?
             * ... reverse it?
             * ... scrub its timeline? (that would maybe be its own thing)
             * ...
             *
             *  Currently it's just a "blend to this state, then blend to this next state" kinda deal.
             *  Maybe proceed like that until it's a liability.
             *
             */
            
            public bool overrideBlendInTime = false;
            public float blendInOverrideTime = 0.1f;

            public float moveTowardsSpeed = 0f;
            public float startTime = 0f;
            public float playbackSpeed = 1f;
            public int index = -1;
        }


        const int MAX_STATE_HANDLES = 16;
        
        const int MAX_ONESHOT_HANDLES = 16;
        
        const float BLEND_EPSILON = 0.001f;
        
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
        private Animator animator;
        private AnimationClipPlayable oneShotPlayable;
        private readonly AnimationLayerMixerPlayable topLevelMixer;
        private readonly AnimationMixerPlayable stateMixer;
        private readonly AnimationMixerPlayable oneShotMixer;
        private readonly AnimationMixerPlayable oneShotAdditiveMixer;
        private readonly AnimationPlayableOutput playableOutput;

        private MethodInfo rebindMethod;
        
        private float currOneShotBlendInTime = -1f;
        
        private float currOneShotBlendOutTime = -1f;
        
        private float currOneShotBlendTime = -1f;

        private bool clipCountChanged = false;

        
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

            //... top level balances between looping states, and one-shots..
            topLevelMixer = AnimationLayerMixerPlayable.Create(graph, 2);

            //... state mixer frantically tries to blend to the most recently requested state:
            stateMixer = AnimationMixerPlayable.Create(graph, MAX_STATE_HANDLES);
            oneShotMixer = AnimationMixerPlayable.Create(graph, MAX_ONESHOT_HANDLES);

            //... oneShot playable sneaks in on top, additively..?

            topLevelMixer.ConnectInput(0, stateMixer, 0);
            topLevelMixer.ConnectInput(1, oneShotMixer, 0);

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
                    clipCountChanged = true;
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
            if (clipCountChanged)
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

            clipCountChanged = false;
        }

        private void TickOneShotBlending()
        {
            if (oneShotClipHandles.Count <= 0)
                return;
            
            bool oneShotClipCountChanged = false;
            
            float heaviestWeight = 0f;
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
                
                // Debug.LogWarning($"oneshot blend weight: {clipHandle.currWeight}");

                if (prevWeight < 1f && clipHandle.currWeight >= 1f)
                {
                    Debug.LogWarning($"Done blending oneshot in, time: {clipHandle.clipPlayable.GetTime()}");
                }
                
                if (leadingClip && clipHandle.currWeight > heaviestWeight)
                {
                    heaviestWeight = clipHandle.currWeight;
                }

                if (clipHandle.callBack != null)
                {
                    if(
                        clipHandle.clipPlayable.GetTime() > clipHandle.callbackTime
                        && clipHandle.clipPlayable.GetPreviousTime() <= clipHandle.callbackTime
                        )
                    {
                        clipHandle.callBack.Invoke();
                    }
                }
                
                if (
                    clipHandle.clipPlayable.GetTime() > clipHandle.clip.length
                    || (!leadingClip && clipHandle.currWeight <= 0f)
                    )
                {
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

        
        //... STATE:
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
            float blendInTime,
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
                clipCountChanged = true;
                var newStateClipHandle = new StateClipHandle()
                {
                    clip = newStateClip,
                    clipPlayable = AnimationClipPlayable.Create(graph, newStateClip),
                    blendInOverrideTime = blendInTime,
                    targetWeight = 1f
                };
                // newStateClipHandle.clipPlayable.SetTraversalMode(Play);
                stateClipHandles.Add(newStateClipHandle);
            }
        }

        
        //... ONE SHOTS:
        public void PlayOneShot(ClipConfig clipConfig)
        {
            PlayOneShot(
                clipConfig.clip,
                clipConfig.blendInTime,
                clipConfig.blendOutTime,
                clipConfig.startTime,
                clipConfig.playbackSpeed
                );
        }

        public void PlayOneShot(AnimationClip clip, float blendInTime, float blendOutTime, float startTime = 0f, float playbackSpeed = 1f)
        {
            if (oneShotClipHandles.Count > 0 && oneShotClipHandles[0].clip == clip)
            {
                Debug.LogWarning($"Already blending to one-shot clip : {clip.name}", clip);
                return;
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

            newOneShotClipHandle.callbackTime = clip.length * 0.5f;
            newOneShotClipHandle.callBack += () => Debug.LogWarning("Halfway callback!");
            
            newOneShotClipHandle.clipPlayable.SetTime(startTime);
            newOneShotClipHandle.clipPlayable.SetSpeed(playbackSpeed);
            newOneShotClipHandle.clipPlayable.SetDuration(clip.length / playbackSpeed);
            newOneShotClipHandle.clipPlayable.Play();

            oneShotMixer.ConnectInput(oneShotClipHandles.Count, newOneShotClipHandle.clipPlayable, 0);
            oneShotMixer.SetInputWeight(oneShotClipHandles.Count, 0f);
            
            oneShotClipHandles.Add(newOneShotClipHandle);
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
            // playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());
            // rebindMethod.Invoke(animator, new object[] { false });

            topLevelMixer.DisconnectInput(1);
            graph.DestroyPlayable(oneShotPlayable);
        }

        
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
        
        
        public void PlayOneShot_OLD(AnimationClip clip, float blendInTime, float blendOutTime)
        {
            if (oneShotPlayable.IsValid() && oneShotPlayable.GetAnimationClip() == clip)
                return;

            InterruptOneShot();

            currOneShotBlendInTime = blendInTime;
            currOneShotBlendOutTime = blendOutTime;
            currOneShotBlendTime = 0f;

            oneShotPlayable = AnimationClipPlayable.Create(graph, clip);
            topLevelMixer.ConnectInput(1, oneShotPlayable, 0);

            var effectiveWeight = blendInTime < 0f ? 1f : 0f;
            topLevelMixer.SetInputWeight(1, effectiveWeight);

            if (rewire)
                playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());

            oneShotPlayable.SetTime(0f);
            oneShotPlayable.Play();
        }
        
        
        // private void TickOneShotBlending_OLD()
        // {
        //     //... we're already done blending, nothing to do.
        //     if (currOneShotBlendTime < 0f)
        //         return;
        //
        //     if (!oneShotPlayable.IsValid() || oneShotPlayable.GetAnimationClip() == null)
        //         return;
        //
        //     var oneShotClip = oneShotPlayable.GetAnimationClip();
        //     
        //     //... we're now done blending, disconnect previous one-shot:
        //     if (currOneShotBlendTime > oneShotClip.length)
        //     {
        //         Debug.LogWarning("Disconnecting one shot");
        //         currOneShotBlendTime = -1f;
        //         currOneShotBlendInTime = -1f;
        //         currOneShotBlendOutTime = -1f;
        //         topLevelMixer.SetInputWeight(1, 0f);
        //         topLevelMixer.SetInputWeight(0, 1f);
        //         DisconnectOneShot();
        //         return;
        //     }
        //
        //     var oneShotWeight =
        //         currOneShotBlendInTime > 0f
        //             ? Mathf.Clamp01(currOneShotBlendTime / currOneShotBlendInTime)
        //             : 1f;
        //
        //     if (currOneShotBlendTime > (oneShotClip.length - currOneShotBlendOutTime))
        //         oneShotWeight = Mathf.Clamp01(1f -
        //                                       (currOneShotBlendTime - (oneShotClip.length - currOneShotBlendOutTime)) /
        //                                       currOneShotBlendOutTime);
        //
        //     topLevelMixer.SetInputWeight(1, oneShotWeight);
        //     // var topLevelBlendWeight = oneShotAdditive ? 1f : 1f - oneShotWeight;
        //     topLevelMixer.SetInputWeight(0, 1f - oneShotWeight);
        //
        //     Debug.LogWarning($"Oneshot Weight : {oneShotWeight}");
        //
        //     currOneShotBlendTime += Time.deltaTime;
        // }
    }
}

