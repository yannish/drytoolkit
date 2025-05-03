using System;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ClipSetter : MonoBehaviour
{
    public Animator parentAnimator;
    public Animator nestedAnimator;

    private PlayableGraph parentGraph;
    private PlayableGraph nestedGraph;
    
    private AnimationPlayableOutput parentOutput;
    private AnimationPlayableOutput nestedOutput;

    private AnimationMixerPlayable parentMixer;
    private AnimationMixerPlayable nestedMixer;

    private AnimationClipPlayable parentClipPlayable;
    private AnimationClipPlayable nestedClipPlayable;
    
    public AnimationClip[] cachedParentClips;
    public AnimationClip[] cachedNestedClips;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var foundAnimators = GetComponentsInChildren<Animator>();
        if (foundAnimators == null || foundAnimators.Length < 1)
            return;
        
        parentAnimator = foundAnimators[0];
        nestedAnimator = foundAnimators[1];
        
        if (parentAnimator == null || nestedAnimator == null)
            return;
        
        cachedParentClips = parentAnimator.runtimeAnimatorController.animationClips;
        cachedNestedClips = nestedAnimator.runtimeAnimatorController.animationClips;

        parentAnimator.runtimeAnimatorController = null;
        nestedAnimator.runtimeAnimatorController = null;

        parentGraph = PlayableGraph.Create("parent");
        nestedGraph = PlayableGraph.Create("nested");
        
        parentOutput = AnimationPlayableOutput.Create(parentGraph, "Animation", parentAnimator);
        nestedOutput = AnimationPlayableOutput.Create(nestedGraph, "Animation", nestedAnimator);
        
        parentMixer = AnimationMixerPlayable.Create(parentGraph, 1);
        nestedMixer = AnimationMixerPlayable.Create(nestedGraph, 1);
        
        parentMixer.SetInputWeight(0, 1f);
        nestedMixer.SetInputWeight(0, 1f);

        parentOutput.SetSourcePlayable(parentMixer);
        nestedOutput.SetSourcePlayable(nestedMixer);
        
        parentGraph.Play();
        nestedGraph.Play();
        
        // GraphVisualizerClient.Show(parentGraph);
        
    }

    public void PlayParentClip(AnimationClip clip)
    {
        Debug.LogWarning($"Playing parent clip : {clip.name}");
        
        parentMixer.DisconnectInput(0);
        
        parentClipPlayable = AnimationClipPlayable.Create(parentGraph, clip);
        parentClipPlayable.Play();
        parentMixer.ConnectInput(0, parentClipPlayable, 0);
        parentMixer.SetInputWeight(0, 1f);
    }

    public void PlayNestedClip(AnimationClip clip)
    {
        Debug.LogWarning($"Playing nested clip : {clip.name}");

        nestedMixer.DisconnectInput(0);
        
        nestedClipPlayable = AnimationClipPlayable.Create(nestedGraph, clip);
        nestedClipPlayable.Play();
        nestedMixer.ConnectInput(0, nestedClipPlayable, 0);
        nestedMixer.SetInputWeight(0, 1f);
    }

    private void OnDestroy()
    {
        if(parentGraph.IsValid())
            parentGraph.Destroy();
        
        if(nestedGraph.IsValid())
            nestedGraph.Destroy();
    }
}
