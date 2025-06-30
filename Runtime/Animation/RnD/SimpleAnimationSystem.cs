using System.Runtime.CompilerServices;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SimpleAnimationSystem
{
    public const float BLEND_EPSILON = 0.001f;

    private readonly Animator animator;
    public PlayableGraph graph { get; private set; }
    private readonly AnimationPlayableOutput playableOutput;
    private readonly AnimationLayerMixerPlayable topLevelMixer;
    private readonly AnimationLayerMixerPlayable stateClipMixer;

    private SimpleClipMixer[] stateClipMixers;
    
    public SimpleAnimationSystem(Animator animator, int layerCount)
    {
        graph = PlayableGraph.Create(animator.gameObject.name + " - SYSTEM");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        
        playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
        topLevelMixer = AnimationLayerMixerPlayable.Create(graph, 3);
        stateClipMixer = AnimationLayerMixerPlayable.Create(graph, layerCount);
        
        topLevelMixer.ConnectInput(0, stateClipMixer, 0);
        topLevelMixer.SetInputWeight(0, 1f);
        stateClipMixer.SetInputWeight(0, 1f);
        
        graph.Play();
    }

    public void Tick()
    {
        if (stateClipMixers.IsNullOrEmpty())
            return;
        
        foreach(var stateClipMixer in stateClipMixers)
            stateClipMixer.TickBlending(this);
    }

    public void TransitionToClip(
        AnimationClip clip,
        float blendInTime = 0f,
        float startTime = 0f,
        float playbackSpeed = 1f,
        int layer = 0
        )
    {
        if (clip == null)
            return;

        bool clipAlreadyExists = false;

        for (int i = 0; i < stateClipMixers[layer].clipHandles.Count; i++)
        {
            var clipHandle = stateClipMixers[layer].clipHandles[i];
            if (clip == clipHandle.clip)
            {
                clipAlreadyExists = true;
                clipHandle.targetWeight = 1f;
            }
            else
            {
                clipHandle.targetWeight = 0f;
            }
        }

        if (!clipAlreadyExists)
        {
            stateClipMixers[layer].stateClipCountChanged = true;

            var newClipHandle = new SimpleClipHandle()
            {
                clip = clip,
                clipPlayable = AnimationClipPlayable.Create(graph, clip),
                targetWeight = 1f
            };
            
            newClipHandle.clipPlayable.SetTime(startTime);
            newClipHandle.clipPlayable.SetSpeed(playbackSpeed);
            
            stateClipMixers[layer].clipHandles.Add(newClipHandle);
        }
    }

    public void Destroy()
    {
        if(graph.IsValid())
            graph.Destroy();
    }
}


