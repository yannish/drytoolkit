using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SimpleClipMixer
{
    public AnimationMixerPlayable mixer;
    public List<SimpleClipHandle> clipHandles;

    public float smoothTime = 0.3f;
    public bool stateClipCountChanged = false;

    
    public void TickBlending(SimpleAnimationSystem system)
    {
        float totalWeights = 0f;
        float heaviestWeight = 0f;

        for (int i = clipHandles.Count - 1; i >= 0; i--)
        {
            var stateClipHandle = clipHandles[i];

            stateClipHandle.currWeight = Mathf.SmoothDamp(
                stateClipHandle.currWeight,
                stateClipHandle.targetWeight,
                ref stateClipHandle.blendVel,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            // switch (blendStyle)
            // {
            //     case AnimationSystem.ClipBlendStyle.SMOOTHDAMP:
            //         
            //         break;
            //
            //     case AnimationSystem.ClipBlendStyle.MOVETOWARDS:
            //         stateClipHandle.currWeight = Mathf.MoveTowards(
            //             stateClipHandle.currWeight,
            //             stateClipHandle.targetWeight,
            //             currStateSmoothDampTime * Time.deltaTime
            //         );
            //         break;
            // }

            if (stateClipHandle.currWeight > heaviestWeight)
                heaviestWeight = stateClipHandle.currWeight;

            // Debug.LogWarning($"... blending clip {stateClipHandle.clip.name} in {stateClipHandle.blendInTime}");     

            if (stateClipHandle.targetWeight < 1f && stateClipHandle.currWeight < SimpleAnimationSystem.BLEND_EPSILON)
            {
                // if(system.logDebug)
                //     Debug.LogWarning($"Removing state clip : {stateClipHandle.clip.name}");

                stateClipCountChanged = true;
                clipHandles.RemoveAt(i);
                system.graph.DestroyPlayable(stateClipHandle.clipPlayable);
            }
            else
            {
                totalWeights += stateClipHandle.currWeight;
            }
        }

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

            // if(system.rebind)
            //     system.playableOutput.SetSourcePlayable(system.playableOutput.GetSourcePlayable());
        
            // system.graph.Evaluate(0f);
        }

        for (int i = 0; i < clipHandles.Count; i++)
        {
            var effectiveWeight = clipHandles[i].currWeight * oneOverTotalWeights;// * normalizeWeight;
            mixer.SetInputWeight(i, effectiveWeight);
        }
        
        stateClipCountChanged = false;
    }
}
