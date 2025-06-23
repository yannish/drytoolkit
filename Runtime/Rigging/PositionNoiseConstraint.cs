
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;


[BurstCompile]
public struct PositionNoiseJob : IWeightedAnimationJob
{
    public ReadWriteTransformHandle constrained;
    
    // public ReadOnlyTransformHandle source;
    
    public FloatProperty noiseSpeed;

    public FloatProperty noiseRep;
    
    public FloatProperty noiseMagnitude;
    
    public bool isPreviewing;

    
    private float dt;

    private const float period = 10000f;

    public float3 periodVec;
    
    
    public void ProcessAnimation(AnimationStream stream)
    {
        if (isPreviewing)
        {
            AnimationRuntimeUtils.PassThrough(stream, constrained);
            return;
        }
        
        float w = jobWeight.Get(stream);

        if (w > 0f)
        {
            dt += stream.deltaTime * noiseSpeed.Get(stream);

            var samplePosA = new float3(dt, 0f, 0f);
            var samplePosB = new float3(0f, dt, 0f);
            var samplePosC = new float3(0f, 0f, dt);
        
            periodVec = new float3(noiseRep.Get(stream), noiseRep.Get(stream), noiseRep.Get(stream));

            var noiseSampleA = noise.pnoise(samplePosA, periodVec);// * 2f - 1f;
            var noiseSampleC = noise.pnoise(samplePosC, periodVec);// * 2f - 1f;
            var noiseSampleB = noise.pnoise(samplePosB, periodVec);// * 2f - 1f;
        
            // Debug.LogWarning($"noiseSampleA: {noiseSampleA}");

            var offset = new Vector3(noiseSampleA, noiseSampleB, noiseSampleC) * noiseMagnitude.Get(stream);// * w;

            var currWorldPos = constrained.GetPosition(stream);

            var offsetPos = currWorldPos + offset;

            constrained.SetPosition(stream, Vector3.Lerp(currWorldPos, offsetPos, w));// onstrained.GetPosition(stream) + offset));
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


[System.Serializable]
public struct PositionNoiseData : IAnimationJobData
{
    public Transform constrainedObject;
    
    // [SyncSceneToStream] public Transform sourceObject;
    
    [SyncSceneToStream] public float noiseSpeed;
    
    [SyncSceneToStream] public float noiseMagnitude;
    
    [SyncSceneToStream] public float noiseRep;
    
    public bool IsValid() => constrainedObject != null;// && sourceObject != null;

    public void SetDefaultValues()
    {
        constrainedObject = null;
        // sourceObject = null;
    }
}


public class PositionNoiseConstraintBinder : AnimationJobBinder<PositionNoiseJob, PositionNoiseData>
{
    public override PositionNoiseJob Create(Animator animator, ref PositionNoiseData data, Component component)
    {
        var job = new PositionNoiseJob();

        job.constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
        
        job.isPreviewing = !Application.isPlaying;
        
        // job.source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject);

        job.noiseSpeed = FloatProperty.Bind(
            animator, 
            component,
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.noiseSpeed))
            );
        
        job.noiseMagnitude = FloatProperty.Bind(
            animator, 
            component,
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.noiseMagnitude))
            );
        
        job.noiseRep = FloatProperty.Bind(
            animator,
            component,
            ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.noiseRep))
            );
        
        job.periodVec = new float3(10000f, 10000f, 10000f);
        
        return job;
    }

    public override void Destroy(PositionNoiseJob job)
    {
        
    }
}


public class PositionNoiseConstraint : RigConstraint<
    PositionNoiseJob,
    PositionNoiseData,
    PositionNoiseConstraintBinder
>
{
    
}