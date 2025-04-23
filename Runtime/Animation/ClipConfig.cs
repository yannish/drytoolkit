using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace drytoolkit.Runtime.Animation
{
    [CreateAssetMenu(menuName = "AnimationSystem/ClipConfig")]
    public class ClipConfig : ScriptableObject
    {
        [Serializable]
        public class ClipConfigEvent
        {
            [Range(0f,1f)]
            public float time;
            public ClipEventDefinition clipEventDefinition;
        }
        
        [Expandable]
        public AnimationClip clip;

        public WrapMode wrapMode = WrapMode.Once;
       
        public float startTime = 0f;
        public float playbackSpeed = 1f;
        
        [Header("For state clips:")]
        public float smoothDampBlendTime = 0.1f;
        public float moveTowardsBlendTime = 1f;
        
        // public bool overrideBlendInTime = false;
        // [ShowIf("overrideBlendInTime")]
        [Header("For one-shot clips:")]
        public float blendInTime = 0.1f;

        // public bool overrideBlendOutTime = false;
        // [ShowIf("overrideBlendOutTime")]
        public float blendOutTime = 0.1f;

        public float moveTowardsSpeed = 0f;


        public bool isAdditive = false;

        [ShowIf("isAdditive")]
        public AnimationClip referencePoseClip;
        [ShowIf("isAdditive")]
        public float referencePoseTime = 0f;
        
        // [Header("EVENTS:")]
        [Space(10)]
        public List<ClipConfigEvent> events = new List<ClipConfigEvent>();

        
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isAdditive && referencePoseClip != null)
            {
                // Debug.LogWarning(".. set ref pose clip.");
                AnimationUtility.SetAdditiveReferencePose(clip, referencePoseClip, referencePoseTime);
            }
            else
            {
                // Debug.LogWarning(".. unset ref pose clip.");
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.hasAdditiveReferencePose = false;
                settings.additiveReferencePoseClip = null;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
        }
#endif
    }
}