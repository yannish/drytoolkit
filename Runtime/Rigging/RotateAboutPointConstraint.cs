using System;
// using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace drytoolkit.Runtime.Rigging
{
    public struct RotateAboutPointJob : IWeightedAnimationJob
    {
        public ReadWriteTransformHandle constrained;
        public ReadOnlyTransformHandle source;

        public AffineTransform sourceOffset;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            //
            var w = jobWeight.Get(stream);
            if (w > 0f)
            {
                Vector3 constrainedPos = constrained.GetPosition(stream);
                Quaternion constrainedRot = constrained.GetRotation(stream);

                Vector3 sourcePos = source.GetPosition(stream);
                Quaternion sourceRot = source.GetRotation(stream); // Use world rotation for consistent space

                // Calculate offset from source to constrained
                Vector3 sourceToConstrained = constrainedPos - sourcePos;

                // Rotate that offset by the weighted source rotation
                Quaternion weightedRot = Quaternion.Slerp(Quaternion.identity, sourceRot, w);
                Vector3 rotatedOffset = weightedRot * sourceToConstrained;

                Vector3 newPos = sourcePos + rotatedOffset;
                Vector3 blendedPos = Vector3.Lerp(constrainedPos, newPos, w);

                Quaternion newRot = Quaternion.Slerp(constrainedRot, sourceRot, w);

                constrained.SetPosition(stream, blendedPos);
                constrained.SetRotation(stream, newRot);
            }
            else
            {
                AnimationRuntimeUtils.PassThrough(stream, constrained);
            }
            
            // if (w > 0)
            // {
            //     var constrainedPos = constrained.GetPosition(stream);
            //     var constrainedRot = constrained.GetRotation(stream);
            //     
            //     var sourcePos = source.GetPosition(stream);
            //     var sourceRot = source.GetLocalRotation(stream);
            //     
            //     //... TODO: for now, just using localRot of source pivot... maybe something smart can be done tho?
            //     
            //     // constrained.GetGlobalTR(stream, out Vector3 conPos, out Quaternion conRot);
            //     // var constrainedTx = new AffineTransform(conPos, conRot);
            //     //
            //     // source.GetGlobalTR(stream, out Vector3 srcPos, out Quaternion srcRot);
            //     // var sourceTx = new AffineTransform(srcPos, srcRot);
            //
            //     // sourceTx *= sourceOffset;
            //     
            //     // var fromTo = constrainedTx.InverseMul(sourceTx);
            //     // var fromTo = sourceTx.InverseMul(constrainedTx);
            //     
            //     var weightedRot = Quaternion.Slerp(Quaternion.identity, sourceRot, w);
            //     
            //     var sourceToConstrained = constrainedPos - sourcePos;
            //     Vector3 rotatedOffset = sourceRot * sourceToConstrained;
            //
            //     var weightedPos = Vector3.Slerp(constrainedPos, sourcePos + rotatedOffset, w);
            //     
            //     constrained.SetPosition(stream, weightedPos);
            //     constrained.SetRotation(stream, weightedRot * constrainedRot);
            // }
            // else
            // {
            //     AnimationRuntimeUtils.PassThrough(stream, constrained);
            // }
        }

        public void ProcessRootMotion(AnimationStream stream)
        {
        
        }

        public FloatProperty jobWeight { get; set; }
    }

    [Serializable]
    public struct RotateAboutPointData : IAnimationJobData
    {
        [SyncSceneToStream]
        public Transform sourceObject;

        [SyncSceneToStream]
        public Transform constrainedObject;
    
        public bool IsValid() => sourceObject != null && constrainedObject != null;

        public void SetDefaultValues()
        {
            sourceObject = null;
            constrainedObject = null;
        }
    }

    public class RotateAboutPointBinder : AnimationJobBinder<RotateAboutPointJob, RotateAboutPointData>
    {
        public override RotateAboutPointJob Create(Animator animator, ref RotateAboutPointData data, Component component)
        {
            var job = new RotateAboutPointJob();
            
            job.constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
            job.source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject);

            var constrainedTransform = data.constrainedObject.transform;
            var constrainedTx = new AffineTransform(constrainedTransform.position, constrainedTransform.rotation);
            
            var sourceTransform = data.sourceObject.transform;
            var sourceTx = new AffineTransform(sourceTransform.position, sourceTransform.rotation);
            
            var sourceOffset = AffineTransform.identity;

            var tmp = sourceTx.InverseMul(constrainedTx);

            sourceOffset.rotation = tmp.rotation;
            
            job.sourceOffset = sourceOffset;
            
            return job;
        }

        public override void Destroy(RotateAboutPointJob job)
        {
            
        }
    }

    public class RotateAboutPointConstraint : RigConstraint<
        RotateAboutPointJob,
        RotateAboutPointData,
        RotateAboutPointBinder
        >
    {
    
    }
}