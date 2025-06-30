using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class LayeredClip
{
    [HorizontalGroup("FIELDS")]
    public AnimationClip clip;
    [HorizontalGroup("FIELDS")]
    public int layer = 0;
}

public class SimpleClipPlayer : MonoBehaviour
{
    public SimpleAnimationSystem simpleAnimationSystem;

    public int layerCount = 1;
    
    public DirectorUpdateMode mode;
    
    private PlayableGraph graph;

    private AnimationPlayableOutput playableOutput;
    
    private AnimationMixerPlayable mixer;
    
    public List<LayeredClip> layeredClips = new List<LayeredClip>();
    
    // public List<AnimationClip> clips;
    
    private Animator animator;

    [Range(0f, 1f)]
    public float weight = 1f;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            return;

        simpleAnimationSystem = new SimpleAnimationSystem(animator, layerCount);
        

    }

    void BasicInit()
    {
        graph = PlayableGraph.Create(animator.gameObject.name + " - Simple Clip Player");
        
        playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        playableOutput.SetSourcePlayable(mixer, 0);
        
        graph.SetTimeUpdateMode(mode);
        
        graph.Play();
    }

    public void PlayClip(AnimationClip clip)
    {
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        mixer.DisconnectInput(0);
        mixer.ConnectInput(0, clipPlayable, 0);
        mixer.SetInputWeight(0,1f);
        playableOutput.SetSourcePlayable(clipPlayable, 0);
    }

    void Update()
    {
        simpleAnimationSystem.Tick();
    }

    private void OnDestroy()
    {
        if(graph.IsValid())
            graph.Destroy();
    }
}
