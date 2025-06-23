using System;using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

public enum Direction
{
    Forward,
    Backward,
    Right,
    Left,
    Up,
    Down,
}

public static class TransformExtensions
{
    public static Vector3 GetDirection(this Transform transform, Direction direction)
    {
        return direction switch
        {
            Direction.Forward => transform.forward,
            Direction.Backward => -transform.forward,
            Direction.Right   => transform.right,
            Direction.Left      => -transform.right,
            Direction.Up      => transform.up,
            Direction.Down     => -transform.up,
            _ => Vector3.zero
        };
    }
}

[BurstCompile]
public struct ConfigurableLookConstraintJob : IWeightedAnimationJob
{
    public static Dictionary<Direction, Vector3> dirLookup = new Dictionary<Direction, Vector3>()
    {
        { Direction.Forward, Vector3.forward },
        { Direction.Backward, Vector3.back},
        { Direction.Left, Vector3.left },
        { Direction.Right, Vector3.right },
        { Direction.Up, Vector3.up },
        { Direction.Down, Vector3.down },
    };

    public Vector3Property angleOffset;
    
    public ReadWriteTransformHandle constrainedObject;
    
    public ReadWriteTransformHandle firstAxisSourceObject;
    
    public ReadWriteTransformHandle secondAxisSourceObject;
    
    public ReadWriteTransformHandle positionSourceObject;

    public IntProperty firstSourceAxis;
    
    public IntProperty secondSourceAxis;
    
    
    public void ProcessAnimation(AnimationStream stream)
    {
        // Vector3 v1Dir;
        // Vector3 v2Dir;
        
        // Vector3 forward = firstAxisSourceObject.GetRotation(stream) * Vector3.forward;
        
        Vector3 v1 = firstAxisSourceObject.GetRotation(stream) * dirLookup[(Direction)firstSourceAxis.Get(stream)];
        Vector3 v2 = secondAxisSourceObject.GetRotation(stream) * dirLookup[(Direction)secondSourceAxis.Get(stream)];
        
        Vector3 crossDir = Vector3.Cross(v1, v2);
        
        Quaternion angleOffsetRot = Quaternion.Euler(angleOffset.Get(stream));
        
        constrainedObject.SetRotation(stream, Quaternion.LookRotation(v1, crossDir) * angleOffsetRot);
        constrainedObject.SetPosition(stream, positionSourceObject.GetPosition(stream));
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
        
    }

    public FloatProperty jobWeight { get; set; }
}

[Serializable]
public struct ConfigurableLookConstraintData : IAnimationJobData
{
    [SyncSceneToStream]    public Vector3 angleOffset;
    
    [SyncSceneToStream] public Transform constrainedObject;
    
    //... CROSS:
    [SyncSceneToStream] public Transform firstAxisSourceObject;
    [SyncSceneToStream] public Direction firstSourceAxis;

    [SyncSceneToStream] public Transform secondAxisSourceObject;
    [SyncSceneToStream] public Direction secondSourceAxis;
    
    [SyncSceneToStream] public Transform positionSourceObject;
    
    public bool IsValid()
    {
        return !(
            constrainedObject == null 
            || firstAxisSourceObject == null 
            || secondAxisSourceObject == null 
            || positionSourceObject == null
        );
    }

    public void SetDefaultValues()
    {
        constrainedObject = null; 
        firstAxisSourceObject = null; 
        secondAxisSourceObject = null; 
        positionSourceObject = null;
    }
}

public class ConfigurableLookConstraintBinder : AnimationJobBinder<ConfigurableLookConstraintJob, ConfigurableLookConstraintData>
{
    public override ConfigurableLookConstraintJob Create(Animator animator, ref ConfigurableLookConstraintData data, Component component)
    {
        var job = new ConfigurableLookConstraintJob();
        job.constrainedObject = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
        job.firstAxisSourceObject = ReadWriteTransformHandle.Bind(animator, data.firstAxisSourceObject);
        job.secondAxisSourceObject = ReadWriteTransformHandle.Bind(animator, data.secondAxisSourceObject);
        job.positionSourceObject = ReadWriteTransformHandle.Bind(animator, data.positionSourceObject);
        job.firstSourceAxis = IntProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.firstSourceAxis)));
        job.secondSourceAxis = IntProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.secondSourceAxis)));
        job.angleOffset = Vector3Property.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.angleOffset)));
        return job;
    }

    public override void Destroy(ConfigurableLookConstraintJob job)
    {
        
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Configurable Look Constraint")]
public class ConfigurableLookConstraint : RigConstraint<
    ConfigurableLookConstraintJob,
    ConfigurableLookConstraintData,
    ConfigurableLookConstraintBinder
    >
{
    
}
