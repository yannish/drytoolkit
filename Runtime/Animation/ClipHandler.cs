using System;
using System.Collections.Generic;
using System.ComponentModel;
using drytoolkit.Runtime.Animation;
using UnityEngine;
using UnityEngine.Serialization;

namespace drytoolkit.Runtime.Animation
{
    public class ClipHandler : MonoBehaviour, IAnimationSystemProvider
    {
        public bool logDebug;
        public AnimationSystem.ClipBlendStyle blendStyle;
        
        [Header("STATE CLIPS:")]
        public int layerCount = 1;
        public List<AnimationClip> stateClips = new List<AnimationClip>();
        [FormerlySerializedAs("stateClips")] public List<ClipConfig> stateClipConfigs;
        public List<AvatarMask> stateMasks;

        [FormerlySerializedAs("oneShotClips")]
        [Header("ONE SHOTS:")]
        public float oneShotBlendIn;
        public float oneShotBlendOut;
        public List<AnimationClip> oneShotClips = new List<AnimationClip>();
        public List<ClipConfig> oneShotClipConfigs;
        [FormerlySerializedAs("additiveOneShotClips")] public List<ClipConfig> additiveOneShotClipConfigs;
        public List<AnimationClip> additiveOneShotClips;
        [Expandable] public ClipConfig oneShotWithEvent;
        [Expandable] public ClipConfig anotherOneShotWithEvent;

        [Header("SEQUENCES:")]
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

        private bool initialized;
        
        void Start()
        {
            Debug.LogWarning("Start on cliphandler.");
            InitializeAnimationSystem();
        }

        void InitializeAnimationSystem()
        {
            if (initialized)
                return;
            
            animator = GetComponent<Animator>();
            if (animator == null)
                return;
            
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
            initialized = true;
        }
        
        public AnimationSystem GetAnimationSystem()
        {
            InitializeAnimationSystem();
            return animSystem;
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