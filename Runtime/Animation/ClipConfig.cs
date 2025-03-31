using UnityEngine;
using UnityEngine.Animations;

namespace drytoolkit.Runtime.Animation
{
    [CreateAssetMenu(menuName = "AnimationSystem/ClipConfig")]
    public class ClipConfig : ScriptableObject
    {
        public AnimationClip clip;
        public AnimationClipPlayable clipPlayable;
        
        /*
         * Who owns blend-in time generally... the system, or the clip...?
         */
        public bool overrideBlendInTime = false;
        public float blendInTime = 0.1f;

        public bool overrideBlendOutTime = false;
        public float blendOutTime = 0.1f;

        public bool isOneShot = false;
        public bool additive = false;
        
        public float startTime = 0f;
        public float playbackSpeed = 1f;
        
        public float moveTowardsSpeed = 0f;
        public float targetWeight = 0f;
        
        
        // public float blendVel = 0f;
        // public float currWeight = 0f;
        // public int index = -1;
    }
}