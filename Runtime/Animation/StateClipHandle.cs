using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class StateClipHandle : ClipHandle //... TODO: return a reference to this when it's created... & do stuff.
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
        // public float blendInOverrideTime = 0.1f;

        // public float moveTowardsSpeed = 0f;
        public float startTime = 0f;
        public float playbackSpeed = 1f;
        public int index = -1;
    }

    [Serializable]
    public class StateClipMixer
    {
        public AnimationMixerPlayable mixer;

        public AnimationSystem.ClipBlendStyle blendStyle;
        public float currSmoothTime = 0.2f;
        public float currMoveTowardsSpeed = 1f;

        [FormerlySerializedAs("stateClipHandles_PREV")]
        public List<StateClipHandle> clipHandles;

        public float currStateSmoothDampTime = 0.2f;

        public bool stateClipCountChanged = false;
        
        public void TickStateBlending(AnimationSystem system)
        {
            float totalWeights = 0f;
            float heaviestWeight = 0f;
            
            for (int i = clipHandles.Count - 1; i >= 0; i--)
            {
                var stateClipHandle = clipHandles[i];

                switch (blendStyle)
                {
                    case AnimationSystem.ClipBlendStyle.SMOOTHDAMP:
                        stateClipHandle.currWeight = Mathf.SmoothDamp(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            ref stateClipHandle.blendVel,
                            currSmoothTime,
                            // currStateSmoothDampTime,
                            // stateClipHandle.smoothBlendTime,
                            Mathf.Infinity,
                            Time.deltaTime
                        );
                        break;

                    case AnimationSystem.ClipBlendStyle.MOVETOWARDS:
                        stateClipHandle.currWeight = Mathf.MoveTowards(
                            stateClipHandle.currWeight,
                            stateClipHandle.targetWeight,
                            currMoveTowardsSpeed * Time.deltaTime
                        // stateClipHandle.moveTowardsSpeed * Time.deltaTime
                            // currStateSmoothDampTime * Time.deltaTime
                        );
                        break;
                }

                if(stateClipHandle.currWeight > heaviestWeight)
                    heaviestWeight = stateClipHandle.currWeight;
                
                // Debug.LogWarning($"... blending clip {stateClipHandle.clip.name} in {stateClipHandle.blendInTime}");     
                
                if (stateClipHandle.targetWeight < 1f && stateClipHandle.currWeight < AnimationSystem.BLEND_EPSILON)
                {
                    if(system.logDebug)
                        Debug.LogWarning($"Removing state clip : {stateClipHandle.clip.name}");
                    stateClipCountChanged = true;
                    clipHandles.RemoveAt(i);
                    system.graph.DestroyPlayable(stateClipHandle.clipPlayable);
                }
                else
                {
                    totalWeights += stateClipHandle.currWeight;
                }
            }

            // var oneOverTotalWeights = 1f / totalWeights;
            var oneOverTotalWeights = clipHandles.Count == 1 ? 1f : 1f / totalWeights;

            //... rewire:
            if (stateClipCountChanged)
            {
                // if (rebind)
                //     rebindMethod.Invoke(animator, new object[] { false });

                for (int i = 0; i < mixer.GetInputCount(); i++)
                {
                    mixer.DisconnectInput(i);
                }

                for (int i = 0; i < clipHandles.Count; i++)
                {
                    mixer.ConnectInput(i, clipHandles[i].clipPlayable, 0);
                }

                if(system.rebind)
                    system.playableOutput.SetSourcePlayable(system.playableOutput.GetSourcePlayable());
                
                // system.graph.Evaluate(0f);
            }

            for (int i = 0; i < clipHandles.Count; i++)
            {
                var effectiveWeight = clipHandles[i].currWeight * oneOverTotalWeights;// * normalizeWeight;
                mixer.SetInputWeight(i, effectiveWeight);
                
                // var normalizeWeight = stateClipHandles.Count > 1 ? oneOverTotalWeights : 1f;
                // Debug.LogWarning($"effStateWeight: {effectiveWeight}");
            }

            // var effectiveStateWeight = stateClipHandles.Count == 1 ? heaviestWeight : 1f;
            // topLevelMixer.SetInputWeight(0, effectiveStateWeight);
                        
            // if(logDebug)
            //     Debug.LogWarning($"... STATE-WEIGHT: {effectiveStateWeight}");
            
            stateClipCountChanged = false;
        }
    }
}
