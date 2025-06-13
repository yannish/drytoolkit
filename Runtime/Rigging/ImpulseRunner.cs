using System;
using drytoolkit.Runtime.Animation;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ImpulseRunner : MonoBehaviour
{
    [Header("SLOW TWITCH:")]
    [Expandable] public SecondOrderTransformConstraint constraint;
    public Vector3 impulse = Vector3.forward;
    public Vector3 torque = Vector3.zero;

    public NativeReference<Vector3> someNativeVector3;
    
    private NativeReference<Vector3> _velocity;
    private NativeReference<Vector3> _torque;
    
    
    [Header("FAST TWITCH:")]
    [Expandable] public SecondOrderTransformConstraint fastTwitchConstraint;
    public Vector3 fastImpulse = Vector3.forward;
    public Vector3 fastTorque = Vector3.zero;
    
    private NativeReference<Vector3> _fastVelocity;
    private NativeReference<Vector3> _fastTorque;

    private AnimationSystem animSystem;


    void OnEnable()
    {
        Debug.LogWarning("started impulse runner.");

        if (fastTwitchConstraint == null || constraint == null)
            return;
        
        _velocity = new NativeReference<Vector3>(Allocator.Persistent);
        _torque = new NativeReference<Vector3>(Allocator.Persistent);
        
        constraint.data.velocityRef = _velocity;
        constraint.data.torqueRef = _torque;
        
        someNativeVector3 = new NativeReference<Vector3>(Allocator.Persistent);

        _fastVelocity = new NativeReference<Vector3>(Allocator.Persistent);
        _fastTorque = new NativeReference<Vector3>(Allocator.Persistent);
        
        fastTwitchConstraint.data.velocityRef = _fastVelocity;
        fastTwitchConstraint.data.torqueRef = _fastTorque;

        animSystem = new AnimationSystem(GetComponent<Animator>());
        animSystem.rebind = rebind;
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

        if(_velocity.IsCreated)
            _velocity.Dispose();
        
        if (_torque.IsCreated)
            _torque.Dispose(); 
        
        if(_fastVelocity.IsCreated)
            _fastVelocity.Dispose();
        
        if(_fastTorque.IsCreated)
            _fastTorque.Dispose();
    }
    
    

    public ClipConfig stateAClip;
    public void GoToStateA() => animSystem.TransitionToState(stateAClip);

    public ClipConfig stateBClip;
    public void GoToStateB() => animSystem.TransitionToState(stateBClip);



    [FoldoutGroup("ONE SHOT")] public bool rebind = true;
    [FoldoutGroup("ONE SHOT")] public AnimationClip oneShotClip;
    [FoldoutGroup("ONE SHOT")] public float blendOutTime = 0.1f;
    [FoldoutGroup("ONE SHOT")] public float blendInTime = 0.1f;
    [ResponsiveButtonGroup("ONE SHOT/ONE SHOT GROUP")]
    public void PlayOneShot() => animSystem.PlayOneShot(oneShotClip);

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
    public void PrimaryTorque() => ApplyTorque(torque);
    private void ApplyTorque(Vector3 torqueToApply) => _torque.Value += torqueToApply;

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryImpulse() => ApplyImpulse(impulse);
    private void ApplyImpulse(Vector3 impulseToApply) => _velocity.Value += impulseToApply;

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryTorqueAndImpulse()
    {
        ApplyTorque(torque);
        ApplyImpulse(impulse);
    }

    [ResponsiveButtonGroup("PRIMARY/PRIMARY GROUP")]
    public void PrimaryWithOneShot()
    {
        PlayOneShot();
        PrimaryTorqueAndImpulse();
    }
    
    
    [FoldoutGroup("SECONDARY")]
    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryTorque() => ApplySecondaryTorque(fastTorque);
    private void ApplySecondaryTorque(Vector3 torque) => _fastTorque.Value += torque;
    
    [ResponsiveButtonGroup("SECONDARY/SECONDARY GROUP")]
    public void SecondaryImpulse() => ApplySecondaryImpulse(fastImpulse);
    private void ApplySecondaryImpulse(Vector3 vector3) => _fastVelocity.Value += vector3;

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
