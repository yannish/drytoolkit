using System;
using UnityEngine;
using UnityEngine.Animations;


namespace drytoolkit.Runtime.Animation
{
    [Serializable]
    public class ClipHandle
    {
        public AnimationSystem.ClipBlendStyle blendStyle;
        public float smoothBlendTime;
        public float moveTowardsSpeed;
        
        public AnimationClip clip;
        public ClipConfig config;
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
}
