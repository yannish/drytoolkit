using System;
using drytoolkit.Runtime.Animation;
using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ImpulseRunner : MonoBehaviour
{
    [Header("SLOW TWITCH:")]
    public SecondOrderTransformConstraint constraint;
    public Vector3 impulse = Vector3.forward;
    public Vector3 torque = Vector3.zero;

    public NativeReference<Vector3> someNativeVector3;
    
    private NativeReference<Vector3> _velocity;
    private NativeReference<Vector3> _torque;
    
    [Header("FAST TWITCH:")]
    public SecondOrderTransformConstraint fastTwitchConstraint;
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
    [ButtonGroup("_animButtons", GroupName = "CLIPS:")]
    public void GoToStateA() => animSystem.TransitionToState(stateAClip);

    public ClipConfig stateBClip;
    [ButtonGroup("_animButtons", GroupName = "CLIPS:")]
    public void GoToStateB() => animSystem.TransitionToState(stateBClip);

    public ClipConfig oneShotClip;
    [ButtonGroup("_animButtons", GroupName = "CLIPS:")]
    public void PlayOneShot() => animSystem.PlayOneShot(oneShotClip);


    [ButtonGroup("_bothButtons", GroupName = "BOTH:")]
    public void PlayOneShotAndImpulse()
    {
        PlayOneShot();
        ApplyBoth();
    }

    [ButtonGroup("SlowButtons", GroupName = "IMPULSE:")]
    public void DoTorque() => ApplyTorque(torque);
    private void ApplyTorque(Vector3 torqueToApply) => _torque.Value += torqueToApply;

    [ButtonGroup("SlowButtons", GroupName = "IMPULSE:")]
    public void DoImpulse() => ApplyImpulse(impulse);
    private void ApplyImpulse(Vector3 impulseToApply)
    {
        Debug.LogWarning("applying impulse.");
        _velocity.Value += impulseToApply;
    }

    [ButtonGroup("SlowButtons", GroupName = "IMPULSE:")]
    public void ApplyBoth()
    {
        ApplyTorque(torque);
        ApplyImpulse(impulse);
    }
    
    
    [ButtonGroup("FastButtons")]
    public void DoFastTorque() => ApplyFastTorque(fastTorque);
    private void ApplyFastTorque(Vector3 torque) => _fastTorque.Value += torque;
    
    [ButtonGroup("FastButtons")]
    public void DoFastImpulse() => ApplyFastImpulse(fastImpulse);
    private void ApplyFastImpulse(Vector3 vector3) => _fastVelocity.Value += vector3;

    [ButtonGroup("FastButtons")]
    public void ApplyFastBoth()
    {
        DoFastImpulse();
        DoFastTorque();
    }

    [Button]
    public void BothImpulsesAndOneShot()
    {
        DoImpulse();
        DoTorque();
        DoFastImpulse();
        DoFastTorque();
        PlayOneShot();
    }

}
