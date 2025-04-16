using System;
using System.Collections.Generic;
using drytoolkit.Runtime.Animation;
using UnityEngine;
using UnityEngine.Serialization;

namespace drytoolkit.Runtime.Animation
{
    public class ClipHandler : MonoBehaviour
    {
        public bool logDebug;
        
        public AnimationSystem.ClipBlendStyle blendStyle;
        
        [Header("CLIPS:")]
        [Expandable]
        public ClipConfig oneShotWithEvent;

        [Expandable]
        public ClipConfig anotherOneShotWithEvent;
        
        public List<ClipConfig> stateClips;
        public List<ClipConfig> oneShotClips;
        public List<ClipConfig> sequenceClips;

        [Header("EVENTS:")]
        public ClipEventDefinition reloadEventDef;
        public ClipEventDefinition midwayEventDef;
        public ClipEventDefinition shotFiredEventDef;
        
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
            Debug.LogWarning($"handling event: {animationEvent.functionName}");
            // SendMessage(animationEvent.messageOptions);
        }

        public void TryPlayWithAnEvent()
        {
            animSystem.PlayOneShot(oneShotWithEvent, () => { Debug.LogWarning("CALLBACK!"); });
            // animSystem.AddListener()
        }

        private void OnDestroy() => animSystem.Destroy();
    }
}   