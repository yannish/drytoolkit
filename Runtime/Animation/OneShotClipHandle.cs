using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;


namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class OneShotClipHandle : ClipHandle
    {
        public WrapMode wrapMode = WrapMode.Once;
        
        public float blendInTime = 0.1f;
        public float blendOutTime = 0.1f;

        public float callbackTime;
        public (float, Action)[] callbacks;
        
        public Action onCompleteCallback;

        public double GetCurrentBlendWeight()
        {
            double currClipTime = clipPlayable.GetTime();

            var clipDuration = clipPlayable.GetDuration();
            // var effLength = clipPlayable.GetDuration();

            double result = 1;
            
            var effBlendInTime = blendInTime * clipPlayable.GetSpeed();
            if (effBlendInTime > 0f && currClipTime <= effBlendInTime)
            {
                result = (currClipTime / effBlendInTime);
                return result;
            }
                // return (float)(currClipTime / effBlendInTime);

            var effBlendOutTime = blendOutTime * clipPlayable.GetSpeed();
            if (blendOutTime > 0f && currClipTime >= (clipDuration - effBlendOutTime))
            {
                result = 1f - math.clamp(
                    (currClipTime - (clipDuration - effBlendOutTime)) / effBlendOutTime,
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
}