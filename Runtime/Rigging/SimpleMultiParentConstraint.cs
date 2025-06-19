using System;
using Unity.Burst;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

[BurstCompile]
public struct SimpleMultiParentConstraintJob : IWeightedAnimationJob
{
    public ReadWriteTransformHandle constrained;
    public ReadOnlyTransformHandle source;
    
    public Quaternion rotationOffset;
    public Vector3 positionOffset;
    
    public void ProcessAnimation(AnimationStream stream)
    {
        float w = jobWeight.Get(stream);
        if (w > 0f)
        {
            source.GetGlobalTR(stream, out Vector3 srcPos, out Quaternion srcRot);
            
            var sourceTx = new AffineTransform(srcPos, srcRot);
            var offsetTx = new AffineTransform(positionOffset, rotationOffset);
            //
            sourceTx *= offsetTx;
            //
            // var accumTx = new AffineTransform(Vector3.zero, QuaternionExt.zero);
            // //
            // accumTx.translation = sourceTx.translation;
            // accumTx.rotation = QuaternionExt.Add(
            //     accumTx.rotation, 
            //     QuaternionExt.Scale(sourceTx.rotation, w)
            //     );
            //
            constrained.GetGlobalTR(stream, out Vector3 conPos, out Quaternion conRot);
            // // var constrainedTx = new AffineTransform(conPos, conRot);
            //
            // Debug.LogWarning($"source pos: {srcPos.x}, {srcPos.y}, {srcPos.z}");

            // Debug.LogWarning($"sourceTx pos: {sourceTx.translation.x}, {sourceTx.translation.y}, {sourceTx.translation.z}");
            
            constrained.SetGlobalTR(
                stream,
                Vector3.Lerp(conPos, sourceTx.translation, w),
                // sourceTx.translation,
                Quaternion.Lerp(conRot, sourceTx.rotation, w)
                // sourceTx.rotation
                );
            //
            // positio
        }
        else
        {
            AnimationRuntimeUtils.PassThrough(stream, constrained);
        }
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
        
    }

    public FloatProperty jobWeight { get; set; }
}

[Serializable]
public struct SimpleMultiParentConstraintData : IAnimationJobData
{
    [SyncSceneToStream] public Transform sourceTransform;
    [SyncSceneToStream] public Transform constrainedTransform;

    [Header("CONFIG:")] 
    public bool useLocalSpace;
    public Transform localSpaceTransform;
    
    public bool IsValid() => sourceTransform != null && constrainedTransform != null;

    public void SetDefaultValues()
    {
        sourceTransform = null;
        constrainedTransform = null;
    }
}


public class SimpleMultiParentConstraintBinder : AnimationJobBinder<
    SimpleMultiParentConstraintJob,
    SimpleMultiParentConstraintData
>
{
    public override SimpleMultiParentConstraintJob Create(Animator animator, ref SimpleMultiParentConstraintData data, Component component)
    {
        var job = new SimpleMultiParentConstraintJob();
        
        var constrainedTx = new AffineTransform(data.constrainedTransform.position, data.constrainedTransform.rotation);
        var sourceTx = new AffineTransform(data.sourceTransform.position, data.sourceTransform.rotation);

        var temp = sourceTx.InverseMul(constrainedTx);
        
        job.positionOffset = temp.translation;
        job.rotationOffset = temp.rotation;
        
        job.constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedTransform);
        job.source = ReadOnlyTransformHandle.Bind(animator, data.sourceTransform);
        
        return job;
    }

    public override void Destroy(SimpleMultiParentConstraintJob job)
    {
        
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/SimpleMultiparentConstraint")]
public class SimpleMultiParentConstraint : RigConstraint<
    SimpleMultiParentConstraintJob,
    SimpleMultiParentConstraintData,
    SimpleMultiParentConstraintBinder
>
{
    
}