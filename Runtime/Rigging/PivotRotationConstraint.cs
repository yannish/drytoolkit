using System;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[BurstCompile]
public struct PivotRotationConstraintJob : IWeightedAnimationJob
{
    public ReadWriteTransformHandle constrained;
    public ReadOnlyTransformHandle source;

    public Vector3 bindOffset; // world-space offset from source to constrained at bind
    public Quaternion bindRotationOffset; // rotation difference at bind pose (in world space)

    public void ProcessAnimation(AnimationStream stream)
    {
        float w = jobWeight.Get(stream);
        if (w <= 0f)
        {
            AnimationRuntimeUtils.PassThrough(stream, constrained);
            return;
        }

        Vector3 sourcePos = source.GetPosition(stream);
        Quaternion sourceRot = source.GetRotation(stream);

        // Calculate target position and rotation based on bind pose offset
        Vector3 targetPos = sourcePos + sourceRot * bindOffset;
        Quaternion targetRot = sourceRot * bindRotationOffset;

        // Blend with current constrained transform
        Vector3 currentPos = constrained.GetPosition(stream);
        Quaternion currentRot = constrained.GetRotation(stream);

        Vector3 blendedPos = Vector3.Lerp(currentPos, targetPos, w);
        Quaternion blendedRot = Quaternion.Slerp(currentRot, targetRot, w);

        constrained.SetPosition(stream, blendedPos);
        constrained.SetRotation(stream, blendedRot);
    }

    public void ProcessRootMotion(AnimationStream stream) { }
    public FloatProperty jobWeight { get; set; }
}


[Serializable]
public struct PivotRotationConstraintData : IAnimationJobData
{
    [SyncSceneToStream]
    public Transform constrained;

    [SyncSceneToStream]
    public Transform source;

    public bool IsValid() => constrained != null && source != null;
    
    public void SetDefaultValues()
    {
        
    }
}

public class PivotRotationConstraintJobBinder : AnimationJobBinder<PivotRotationConstraintJob, PivotRotationConstraintData>
{
    public override PivotRotationConstraintJob Create(Animator animator, ref PivotRotationConstraintData data, Component component)
    {
        var job = new PivotRotationConstraintJob
        {
            constrained = ReadWriteTransformHandle.Bind(animator, data.constrained),
            source = ReadOnlyTransformHandle.Bind(animator, data.source)
        };

        // Compute bind pose data
        var bindConstrainedPos = data.constrained.position;
        var bindConstrainedRot = data.constrained.rotation;

        var bindSourcePos = data.source.position;
        var bindSourceRot = data.source.rotation;

        job.bindOffset = Quaternion.Inverse(bindSourceRot) * (bindConstrainedPos - bindSourcePos); // local offset in source space
        job.bindRotationOffset = Quaternion.Inverse(bindSourceRot) * bindConstrainedRot; // relative rotation

        return job;
    }

    public override void Destroy(PivotRotationConstraintJob job) { }
}


[DisallowMultipleComponent]
[AddComponentMenu("Animation Rigging/Pivot Rotation Constraint")]
public class PivotRotationConstraint : RigConstraint<
    PivotRotationConstraintJob,
    PivotRotationConstraintData,
    PivotRotationConstraintJobBinder>
{ }