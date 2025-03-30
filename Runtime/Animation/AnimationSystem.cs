using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class AnimationSystem
    {
        private Animator animator;
        public PlayableGraph graph { get; private set; }

        public bool rewire = false;
        public bool rebind = false;

        private AnimationClipPlayable oneShotPlayable;
        // private AnimationClipPlayable additiveOneShotPlayable;

        // private readonly AnimationLayerMixerPlayable layerMixer;
        private readonly AnimationLayerMixerPlayable topLevelMixer;
        private readonly AnimationMixerPlayable stateMixer;

        private readonly AnimationPlayableOutput playableOutput;

        const int MAX_STATE_HANDLES = 16;
        const float BLEND_EPSILON = 0.001f;

        public List<StateClipHandle> stateClipHandles = new List<StateClipHandle>();

        public enum ClipBlendStyle
        {
            SMOOTHDAMP,
            MOVETOWARDS
        }

        [Serializable]
        public class OneShotClipHandle
        {
            public AnimationClip clip;
            public bool additive;
            public float blendInTime = 0.1f;
            public float blendOutTime = 0.1f;
        }

        [Serializable]
        public class IdleClip
        {
            public AnimationClip clip;
            public float blendInTime = 0.1f;
        }

        [Serializable]
        public class StateClipHandle
        {
            public AnimationClip clip;
            public AnimationClipPlayable clipPlayable;
            public bool overrideBlendInTime = false;
            public float blendInTime = 0.1f;
            public float moveTowardsSpeed = 0f;
            public float blendVel = 0f;
            public float targetWeight = 0f;
            public float currWeight = 0f;
            public float startTime = 0f;
            public float playbackSpeed = 1f;
            public int index = -1;
        }

        private MethodInfo rebindMethod;

        private float currStateBlendTime = -1f;
        private bool clipCountChanged = false;

        private float currOneShotBlendInTime = -1f;
        private float currOneShotBlendOutTime = -1f;
        private float currOneShotBlendTime = -1f;
        private bool oneShotAdditive = false;

        public AnimationSystem(
            Animator animator, 
            // AnimationClip idleClip = null,
            DirectorUpdateMode mode = DirectorUpdateMode.GameTime
            )
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

            //... oneShot playable sneaks in on top, additively..?

            topLevelMixer.ConnectInput(0, stateMixer, 0);

            playableOutput.SetSourcePlayable(topLevelMixer);

            graph.GetRootPlayable(0).SetInputWeight(0, 1f);

            graph.Play();
        }

        public void Tick(ClipBlendStyle blendStyle = ClipBlendStyle.SMOOTHDAMP, bool normalizeBlendWeights = false)
        {
            TickStateClips(blendStyle);
            TickOneShotBlending();
        }

        private void TickStateClips(ClipBlendStyle blendStyle)
        {
            float totalWeights = 0f;
            for (int i = stateClipHandles.Count - 1; i >= 0; i--)
            {
                var stateClipHandle = stateClipHandles[i];
                var stateClip = stateClipHandle.clip;

                switch (blendStyle)
                {
                    case ClipBlendStyle.SMOOTHDAMP:
                        stateClipHandle.currWeight = Mathf.SmoothDamp(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            ref stateClipHandle.blendVel,
                            currStateBlendTime,
                            // stateClipHandle.blendInTime,
                            Mathf.Infinity,
                            Time.deltaTime
                        );
                        break;

                    case ClipBlendStyle.MOVETOWARDS:
                        stateClipHandle.currWeight = Mathf.MoveTowards(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            currStateBlendTime * Time.deltaTime
                        );
                        break;
                }

                // Debug.LogWarning($"... blending clip {stateClipHandle.clip.name} in {stateClipHandle.blendInTime}");            

                if (stateClipHandle.targetWeight < 1f && stateClipHandle.currWeight < BLEND_EPSILON)
                {
                    clipCountChanged = true;
                    stateClipHandles.RemoveAt(i);

                    Debug.LogWarning($"Removing state clip : {stateClipHandle.clip.name}");
                }
                else
                {
                    totalWeights += stateClipHandle.currWeight;
                }
            }

            var oneOverTotalWeights = 1f / totalWeights;

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

            clipCountChanged = false;
        }

        private void TickOneShotBlending()
        {
            if (currOneShotBlendTime < 0f)
                return;

            if (!oneShotPlayable.IsValid() || oneShotPlayable.GetAnimationClip() == null)
                return;

            var oneShotClip = oneShotPlayable.GetAnimationClip();

            if (currOneShotBlendTime > oneShotClip.length)
            {
                Debug.LogWarning("Disconnecting one shot");
                currOneShotBlendTime = -1f;
                currOneShotBlendInTime = -1f;
                currOneShotBlendOutTime = -1f;
                topLevelMixer.SetInputWeight(1, 0f);
                topLevelMixer.SetInputWeight(0, 1f);
                DisconnectOneShot();
                return;
            }

            var oneShotWeight =
                currOneShotBlendInTime > 0f
                    ? Mathf.Clamp01(currOneShotBlendTime / currOneShotBlendInTime)
                    : 1f;

            if (currOneShotBlendTime > (oneShotClip.length - currOneShotBlendOutTime))
                oneShotWeight = Mathf.Clamp01(1f -
                                              (currOneShotBlendTime - (oneShotClip.length - currOneShotBlendOutTime)) /
                                              currOneShotBlendOutTime);

            topLevelMixer.SetInputWeight(1, oneShotWeight);
            var topLevelBlendWeight = oneShotAdditive ? 1f : 1f - oneShotWeight;
            topLevelMixer.SetInputWeight(0, topLevelBlendWeight);

            Debug.LogWarning($"Oneshot Weight : {oneShotWeight}");

            currOneShotBlendTime += Time.deltaTime;
        }


        //... STATE:
        public void TransitionToStateClip(StateClipHandle stateClipHandle, float blendInTime = -1f,
            float startTime = 0f, float playbackSpeed = 1f)
        {
            TransitionToState(
                stateClipHandle.clip,
                stateClipHandle.overrideBlendInTime || blendInTime < 0f ? stateClipHandle.blendInTime : blendInTime
            );
        }

        public void TransitionToClipConfig(ClipConfig clipConfig)
        {
            if (clipConfig.clip == null)
            {
                Debug.LogWarning("... clip config had no clip assigned!", clipConfig);
                return;
            }

            TransitionToState(
                clipConfig.clip,
                clipConfig.blendInTime
            );
        }

        public void TransitionToState(
            AnimationClip newStateClip,
            float blendInTime,
            float startTime = 0f,
            float playbackSpeed = 1f
            )
        {
            currStateBlendTime = blendInTime;

            bool stateClipAlreadyExists = false;
            for (int i = 0; i < stateClipHandles.Count; i++)
            {
                var stateClipHandle = stateClipHandles[i];
                var stateClip = stateClipHandle.clip;
                if (stateClip == newStateClip)
                {
                    stateClipAlreadyExists = true;
                    stateClipHandle.blendInTime = blendInTime;
                    stateClipHandle.targetWeight = 1f;
                }
                else
                {
                    stateClipHandle.targetWeight = 0f;
                }
            }

            if (!stateClipAlreadyExists)
            {
                clipCountChanged = true;
                var newStateClipHandle = new StateClipHandle()
                {
                    clip = newStateClip,
                    clipPlayable = AnimationClipPlayable.Create(graph, newStateClip),
                    blendInTime = blendInTime,
                    targetWeight = 1f
                };

                stateClipHandles.Add(newStateClipHandle);
                // stateMixer.ConnectInput(stateClipHandles.Count, newStateClipHandle.clipPlayable, 0); 
            }
        }

        //... ONE SHOTS:
        public void PlayOneShot(OneShotClipHandle oneShotClipHandle)
        {
            PlayOneShot(
                oneShotClipHandle.clip,
                oneShotClipHandle.blendInTime,
                oneShotClipHandle.blendOutTime,
                oneShotClipHandle.additive
            );
        }

        public void PlayOneShot(AnimationClip clip, float blendInTime, float blendOutTime, bool additive = false)
        {
            if (oneShotPlayable.IsValid() && oneShotPlayable.GetAnimationClip() == clip)
                return;

            InterruptOneShot();

            currOneShotBlendInTime = blendInTime;
            currOneShotBlendOutTime = blendOutTime;
            oneShotAdditive = additive;

            oneShotPlayable = AnimationClipPlayable.Create(graph, clip);
            topLevelMixer.ConnectInput(1, oneShotPlayable, 0);

            var effectiveWeight = blendInTime < 0f ? 1f : 0f;
            topLevelMixer.SetInputWeight(1, effectiveWeight);

            if (rewire)
                playableOutput.SetSourcePlayable(playableOutput.GetSourcePlayable());

            oneShotPlayable.SetTime(0f);
            oneShotPlayable.Play();

            currOneShotBlendTime = 0f;
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

        public void Destroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }
    }
}

