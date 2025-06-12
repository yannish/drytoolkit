using System;
using System.Collections.Generic;
using System.ComponentModel;
using drytoolkit.Runtime.Animation;
using UnityEngine;
using UnityEngine.Serialization;

namespace drytoolkit.Runtime.Animation
{
    public class ClipHandler : MonoBehaviour
    {
        public bool logDebug;

        public int layerCount = 1;
        
        public AnimationSystem.ClipBlendStyle blendStyle;
        
        [Header("CLIPS:")]
        [Expandable] public ClipConfig oneShotWithEvent;
        [Expandable] public ClipConfig anotherOneShotWithEvent;
        
        public List<ClipConfig> stateClips;
        public List<AvatarMask> stateMasks;
        
        public List<ClipConfig> oneShotClips;
        public List<ClipConfig> additiveOneShotClips;
        public List<ClipConfig> sequenceClips;

        [Header("EVENTS:")]
        public ClipEventDefinition reloadEventDef;
        public ClipEventDefinition midwayEventDef;
        public ClipEventDefinition shotFiredEventDef;
        
        public AnimationSystem animSystem;

        [Header("BLENDING:")]
        public float blendInTime = 0.2f;
        
        [Header("MASKING:")]
        public List<Transform> maskedTransforms = new List<Transform>();
        
        private Animator animator;
        
        
        void Start()
        {
            animator = GetComponent<Animator>();
            animator.runtimeAnimatorController = null;

            foreach (var mask in stateMasks)
            {
                if(mask== null)
                    continue;

                for (int i = 0; i < mask.transformCount; i++)
                {
                    // var path = mask.GetTransformPath(i);
                    // mask.
                    Debug.LogWarning($"path: {mask.GetTransformPath(i)}, {mask.GetTransformActive(i)}");
                }
            }
            
            // stateMasks.Clear();
            // stateMasks.Add(null);
            //
            // var avatarmask = new AvatarMask();
            // for (int i = 0; i < maskedTransforms.Count; i++)
            // {
            //     avatarmask.AddTransformPath(maskedTransforms[i]);
            //     avatarmask.SetTransformActive(i, true);
            // }
            
            // stateMasks.Add(avatarmask);
            
            animSystem = new AnimationSystem(animator, layerCount: layerCount, avatarMasks: stateMasks);
            
            // GraphVisualizerClient.Show(animSystem.graph);
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