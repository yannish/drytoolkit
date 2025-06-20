using System;
using drytoolkit.Runtime.Animation;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public class ImpulseRunner : MonoBehaviour
{
    [Header("SLOW TWITCH:")]
    [Expandable] public SecondOrderTransformConstraint constraint;
    public Vector3 primaryImpulse = Vector3.forward;
    public Vector3 primaryAlignedImpulse = Vector3.forward;
    public Vector3 primaryTorque = Vector3.zero;

    private NativeReference<Vector3> primaryImpulseRef;
    private NativeReference<Vector3> alignedPrimaryImpulseRef;
    private NativeReference<Vector3> primaryTorqueRef;
    
    
    [Header("FAST TWITCH:")]
    [Expandable] public SecondOrderTransformConstraint fastTwitchConstraint;
    public Vector3 fastImpulse = Vector3.forward;
    public Vector3 fastTorque = Vector3.zero;
    
    private NativeReference<Vector3> secondaryImpulse;
    private NativeReference<Vector3> secondaryTorque;

    private AnimationSystem animSystem;


    void OnEnable()
    {
        Debug.LogWarning("started impulse runner.");

        if (fastTwitchConstraint == null || constraint == null)
            return;
        
        primaryImpulseRef = new NativeReference<Vector3>(Allocator.Persistent);
        constraint.data.velocityRef = primaryImpulseRef;
        
        primaryTorqueRef = new NativeReference<Vector3>(Allocator.Persistent);
        constraint.data.torqueRef = primaryTorqueRef;
        
        alignedPrimaryImpulseRef = new NativeReference<Vector3>(Allocator.Persistent);
        constraint.data.alignedImpulseRef = alignedPrimaryImpulseRef;
        
        secondaryImpulse = new NativeReference<Vector3>(Allocator.Persistent);
        fastTwitchConstraint.data.velocityRef = secondaryImpulse;
        
        secondaryTorque = new NativeReference<Vector3>(Allocator.Persistent);
        fastTwitchConstraint.data.torqueRef = secondaryTorque;

        var foundAnimator = GetComponent<Animator>();
        if (foundAnimator != null)
        {
            animSystem = new AnimationSystem(GetComponent<Animator>());
            animSystem.rebind = rebind;
        }
    }

    private void Update()
    {
        if (animSystem == null)
            return;
        animSystem.Tick();
    }
    
    void OnDestroy()
    {
        // if (_velocity.IsCreated)
        //     _velocity.Dispose();
        
        if(animSystem != null)
            animSystem.Destroy();

        // if(primaryImpulseRef.IsCreated)
        //     primaryImpulseRef.Dispose();
        
        // if (primaryTorqueRef.IsCreated)
        //     primaryTorqueRef.Dispose(); 
        //
        // if(secondaryImpulse.IsCreated)
        //     secondaryImpulse.Dispose();
        //
        // if(secondaryTorque.IsCreated)
        //     secondaryTorque.Dispose();
    }
    
    

    public ClipConfig stateAClip;
    public void GoToStateA() => animSystem.TransitionToState(stateAClip);

    public ClipConfig stateBClip;
    public void GoToStateB() => animSystem.TransitionToState(stateBClip);


    [Header("ANIMATION:")] public ClipHandler ClipHandler;

    [FoldoutGroup("ONE SHOT")] public bool rebind = true;
    [FoldoutGroup("ONE SHOT")] public AnimationClip oneShotClip;
    [FoldoutGroup("ONE SHOT")] public float blendOutTime = 0.1f;
    [FoldoutGroup("ONE SHOT")] public float blendInTime = 0.1f;
    [ResponsiveButtonGroup("ONE SHOT/ONE SHOT GROUP")]
    public void PlayOneShot()
    {
        if (ClipHandler != null)
        {
            ClipHandler.animSystem.PlayOneShot(oneShotClip);
            return;
        }
        
        animSystem.PlayOneShot(oneShotClip);
    }

    [ResponsiveButtonGroup("ONE SHOT/ONE SHOT GROUP")]
    public void PlayAll()
    {
        PrimaryImpulse();
        PrimaryTorque();
        SecondaryImpulse();
        SecondaryTorque();
        PlayOneShot();
    }



    
    [FoldoutGroup("PRIMARY")]
    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryTorque() => ApplyTorque(primaryTorque);

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryImpulse() => ApplyImpulse(primaryImpulse);

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryAlignedImpulse()
    {
        ApplyAlignedImpulse(primaryAlignedImpulse);
        // var effectiveImpulse = constraint.data.sourceObject.rotation * impulse;
        // ApplyImpulse(effectiveImpulse);
    }

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryTorqueAndImpulse()
    {
        ApplyTorque(primaryTorque);
        ApplyImpulse(primaryImpulse);
    }

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryTorqueAndAlignedImpulse()
    {
        ApplyAlignedImpulse(primaryAlignedImpulse);
        ApplyTorque(primaryTorque);
    }

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryWithOneShot()
    {
        PlayOneShot();
        PrimaryTorqueAndImpulse();
    }

    private void ApplyAlignedImpulse(Vector3 impulse) => alignedPrimaryImpulseRef.Value += impulse;
    
    private void ApplyTorque(Vector3 torqueToApply) => primaryTorqueRef.Value += torqueToApply;

    private void ApplyImpulse(Vector3 impulseToApply) => primaryImpulseRef.Value += impulseToApply;
    
    
    [FoldoutGroup("SECONDARY")]
    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryTorque() => ApplySecondaryTorque(fastTorque);
    private void ApplySecondaryTorque(Vector3 torque) => secondaryTorque.Value += torque;
    
    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryImpulse() => ApplySecondaryImpulse(fastImpulse);
    private void ApplySecondaryImpulse(Vector3 vector3) => secondaryImpulse.Value += vector3;

    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryTorqueAndImpulse()
    {
        SecondaryImpulse();
        SecondaryTorque();
    }

    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryWithOneShot()
    {
        PlayOneShot();
        SecondaryImpulse();
        SecondaryTorque();
    }
    

}
