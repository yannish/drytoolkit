using System;
using System.Collections.Generic;
using drytoolkit.Runtime.Animation;
using UnityEngine;

namespace drytoolkit.Runtime.Animation
{
    public class ClipHandler : MonoBehaviour
    {
        public bool logDebug;
        
        public AnimationSystem.ClipBlendStyle blendStyle;
        public List<ClipConfig> clips;
        public AnimationSystem animSystem;

        [Header("BLENDING:")]
        public float blendInTime = 0.2f;
        
        private Animator animator;
        
        void Start()
        {
            animator = GetComponent<Animator>();
            animator.runtimeAnimatorController = null;
            animSystem = new AnimationSystem(animator);
            
            GraphVisualizerClient.Show(animSystem.graph);
        }

        private void Update()
        {
            animSystem.Tick(blendStyle);
        }

        public void HandleAnimationEvent(AnimationEvent animationEvent)
        {
            // if(animationEvent.animatorClipInfo.clip == 
        }

        private void OnDestroy() => animSystem.Destroy();
    }
}   