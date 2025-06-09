using Sirenix.OdinInspector;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class SecondOrderImpulser : MonoBehaviour
{
    public Vector3 impulse = Vector3.forward;
    public Vector3 torque = Vector3.zero;
    
    private NativeReference<Vector3> _velocity;
    private NativeReference<Vector3> _torque;
    
    public SecondOrderTransformConstraint constraint; 

    
    void Awake()
    {
        _velocity = new NativeReference<Vector3>(Allocator.Persistent);
        _torque = new NativeReference<Vector3>(Allocator.Persistent);
        
        constraint.data.velocityRef = _velocity;
        constraint.data.torqueRef = _torque;
    }

    [Button]
    public void DoTorque() => ApplyTorque(torque);
    private void ApplyTorque(Vector3 torqueToApply) => _torque.Value += torqueToApply;

    [Button]
    public void DoImpulse() => ApplyImpulse(impulse);
    private void ApplyImpulse(Vector3 impulseToApply) => _velocity.Value += impulseToApply;

    void OnDestroy()
    {
        if (_velocity.IsCreated)
            _velocity.Dispose();
    }
}
