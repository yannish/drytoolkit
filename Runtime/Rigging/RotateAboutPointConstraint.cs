using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;

namespace drytoolkit.Runtime.Rigging
{
    public struct RotateAboutPointJob : IWeightedAnimationJob
    {
        public ReadWriteTransformHandle constrained;
        public ReadOnlyTransformHandle source;
        
        public void ProcessAnimation(AnimationStream stream)
        {
            var w = jobWeight.Get(stream);
            
            var constrainedPos = constrained.GetPosition(stream);
            var constrainedRot = constrained.GetRotation(stream);
            var sourcePos = source.GetPosition(stream);
            var sourceRot = source.GetRotation(stream);
            var weightedRot = Quaternion.Slerp(quaternion.identity, sourceRot, w);
            
            var sourceToConstrained = constrainedPos - sourcePos;
            Vector3 rotatedOffset = sourceRot * sourceToConstrained;

            var weightedPos = Vector3.Slerp(constrainedPos, sourcePos + rotatedOffset, w);
            
            constrained.SetPosition(stream, weightedPos);
            constrained.SetRotation(stream, constrainedRot * weightedRot);

            // Vector3 = 
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