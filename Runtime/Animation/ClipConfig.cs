using UnityEngine;
using UnityEngine.Animations;

namespace drytoolkit.Runtime.Animation
{
    [CreateAssetMenu(menuName = "AnimationSystem/ClipConfig")]
    public class ClipConfig : ScriptableObject
    {
        [Expandable]
        public AnimationClip clip;
        [HideInInspector]
        public AnimationClipPlayable clipPlayable;
        
        public bool overrideLoop = false;
        [ShowIf("overrideLoop")]
        public bool loop = false;
        
        public float startTime = 0f;
        public float playbackSpeed = 1f;
        
        /*
         * Who owns blend-in time generally... the system, or the clip...?
         */

        public float smoothDampBlendTime = 0.1f;
        public float moveTowardsBlendTime = 1f;
        
        public bool overrideBlendInTime = false;
        [ShowIf("overrideBlendInTime")]
        public float blendInTime = 0.1f;

        public bool overrideBlendOutTime = false;
        [ShowIf("overrideBlendOutTime")]
        public float blendOutTime = 0.1f;

        public bool isOneShot = false;
        public bool additive = false;

        public float moveTowardsSpeed = 0f;
        
        // public float blendVel = 0f;
        // public float currWeight = 0f;
        // public int index = -1;
    }
}