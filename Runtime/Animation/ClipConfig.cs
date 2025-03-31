using UnityEngine;
using UnityEngine.Animations;

namespace drytoolkit.Runtime.Animation
{
    [CreateAssetMenu(menuName = "AnimationSystem/ClipConfig")]
    public class ClipConfig : ScriptableObject
    {
        public AnimationClip clip;
        public AnimationClipPlayable clipPlayable;
        public bool overrideBlendInTime = false;
        public float blendInTime = 0.1f;
        
        public float moveTowardsSpeed = 0f;
        public float targetWeight = 0f;
        public float startTime = 0f;
        public float playbackSpeed = 1f;

        // public float blendVel = 0f;
        // public float currWeight = 0f;
        // public int index = -1;
    }

}