using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;

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
        
        public float smoothDampBlendTime = 0.1f;
        public float moveTowardsBlendTime = 1f;
        
        public bool overrideBlendInTime = false;
        [ShowIf("overrideBlendInTime")]
        public float blendInTime = 0.1f;

        public bool overrideBlendOutTime = false;
        [ShowIf("overrideBlendOutTime")]
        public float blendOutTime = 0.1f;

        public float moveTowardsSpeed = 0f;
        
        
        [Header("EVENTS:")]
        public List<ClipConfigEvent> events;
        
        // public float blendVel = 0f;
        // public float currWeight = 0f;
        // public int index = -1;
    }
}