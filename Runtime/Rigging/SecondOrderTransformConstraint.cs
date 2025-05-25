using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using UnityEngine.Video;
using AffineTransform = UnityEngine.Animations.Rigging.AffineTransform;


[BurstCompile]
public struct SecondOrderTransformJob : IWeightedAnimationJob
{
    public NativeReference<Vector3> impulseVel;

    
    const float k_FixedDt = 0.01666667f;

    public ReadWriteTransformHandle constrained;
    public ReadWriteTransformHandle source;
    
    public bool isPreviewing;

    public BoolProperty useLocalSpace;
    public ReadWriteTransformHandle localSpaceTransform;
    
    
    //... POS:
    public FloatProperty maxPositionDistance;
    public FloatProperty frequency;
    public FloatProperty damping;
    public FloatProperty response;

    public Vector3 currPos;
    public Vector3 currVel;

    public Vector3 currTargetPos;
    public Vector3 prevTargetPos;
    
    public float k1, k2, k3;
    
    
    //... ROT:
    public FloatProperty maxRotationAngle;
    public FloatProperty rotFrequency;
    public FloatProperty rotDamping;
    public FloatProperty rotResponse;

    public Quaternion currRot;
    public Quaternion currTargetRot;
    
    public float rotK1, rotK2, rotK3;
    
    public SecondOrderFloat xDyn;
    public SecondOrderFloat yDyn;
    public SecondOrderFloat zDyn;
    public SecondOrderFloat wDyn;
    
    
    //... PER-AXIS ROT:
    public Vector3Property perAxisFrequency;
    public Vector3Property perAxisDamping;
    public Vector3Property perAxisResponse;

    
    public void ProcessAnimation(AnimationStream stream)
    {
        if (isPreviewing)
        {
            constrained.SetPosition(stream, source.GetPosition(stream));
            constrained.SetRotation(stream, source.GetRotation(stream));
            AnimationRuntimeUtils.PassThrough(stream, constrained);
            return;
        }

        float w = jobWeight.Get(stream);
        float streamDt = Mathf.Abs(stream.deltaTime);

        if (streamDt <= 0f)
            return;
        
        // if(w <= 0f || streamDt <= 0f)
        //     return;

        // var posDamping = damping.Get(stream);
        // var posFrequency = frequency.Get(stream);
        // var posResponse = response.Get(stream);
        

        k1 = damping.Get(stream) / (math.PI * frequency.Get(stream));
        k2 = 1f / math.pow(2f * math.PI * frequency.Get(stream), 2f);
        k3 = response.Get(stream) * damping.Get(stream) / (2 * math.PI * frequency.Get(stream));
        
        rotK1 = rotDamping.Get(stream) / (math.PI * rotFrequency.Get(stream));
        rotK2 = 1f / math.pow(2f * math.PI * rotFrequency.Get(stream), 2f);
        rotK3 = rotResponse.Get(stream) * rotDamping.Get(stream) / (2 * math.PI * rotFrequency.Get(stream));
        
        bool useLocalSpace = this.useLocalSpace.Get(stream) && localSpaceTransform.IsValid(stream);

        // prevTargetRot = currTargetRot;
        var targetPos = source.GetPosition(stream);
        if (useLocalSpace)
        {
            localSpaceTransform.GetGlobalTR(stream, out var localTxPos, out var localTxRot);
            var localTransformMatrix = new AffineTransform(localTxPos, localTxRot);
            targetPos = localTransformMatrix.InverseTransform(targetPos);
        }
        
        prevTargetPos = currTargetPos;
        currTargetPos = targetPos;
        
        currTargetRot = source.GetRotation(stream);
        if (useLocalSpace)
        {
            currTargetRot = Quaternion.Inverse(localSpaceTransform.GetRotation(stream)) * currTargetRot;
        }

        var effectiveDt = Mathf.Abs(streamDt);
        
        // var perAxisFreq = perAxisFrequency.Get(stream);
        // var perAxisDamp = perAxisDamping.Get(stream);
        // var perAxisResp = perAxisResponse.Get(stream);
        // var perAxisParams = GetPerAxisParams(perAxisFreq, perAxisDamp, perAxisResp);
        //
        // var xRotParams = perAxisParams.GetRow(0);
        // var yRotParams = perAxisParams.GetRow(1);
        // var zRotParams = perAxisParams.GetRow(2);
        
        //.. TODO: 
        
        // while (streamDt > 0f)
        // {
            currRot.y = yDyn.Tick(effectiveDt, currTargetRot.y, rotK1, rotK2, rotK3);
            currRot.x = xDyn.Tick(effectiveDt, currTargetRot.x, rotK1, rotK2, rotK3);
            currRot.z = zDyn.Tick(effectiveDt, currTargetRot.z, rotK1, rotK2, rotK3);
            currRot.w = wDyn.Tick(effectiveDt, currTargetRot.w, rotK1, rotK2, rotK3);
            currRot.Normalize();

            currVel += impulseVel.Value;
            impulseVel.Value = Vector3.zero;
            
            var xDeriv = (currTargetPos - prevTargetPos) / effectiveDt;
            currPos += effectiveDt * currVel;
            currVel += effectiveDt * (currTargetPos + k3 * xDeriv - currPos - k1 * currVel) / k2;
            
            // streamDt -= k_FixedDt;
        // }

        if (useLocalSpace)
        {
            constrained.SetLocalRotation(stream, Quaternion.Slerp(currRot, currTargetRot, 1f - w));
            constrained.SetLocalPosition(stream, Vector3.Lerp(currPos, currTargetPos, 1f - w));
        }
        else
        {
            constrained.SetRotation(stream, Quaternion.Slerp(currRot, currTargetRot, 1f - w));
            constrained.SetPosition(stream, Vector3.Lerp(currPos, currTargetPos, 1f - w));
            // constrained.SetRotation(stream, currRot);
            // constrained.SetPosition(stream, currPos);
        }
        
        // Debug.LogWarning($"currTargPos: ({currTargetPos.x}, {currTargetPos.y}, {currTargetPos.z})");

        Matrix4x4 GetPerAxisParams(Vector3 freq, Vector3 damp, Vector3 resp)
        {
            Vector3 perAxisParams = Vector3.zero;
            Matrix4x4 paramMatrix = new Matrix4x4();

            var k1x = damp.x / (math.PI * freq.x);
            var k1y = damp.y / (math.PI * freq.y);
            var k1z = damp.z / (math.PI * freq.z);

            var k2x = 1f / math.pow(2f * math.PI * freq.x, 2f);
            var k2y = 1f / math.pow(2f * math.PI * freq.y, 2f);
            var k2z = 1f / math.pow(2f * math.PI * freq.z, 2f);

            var k3x = resp.x * damp.x / (2f * math.PI * freq.x);
            var k3y = resp.y * damp.y / (2f * math.PI * freq.y);
            var k3z = resp.z * damp.z / (2f * math.PI * freq.z);

            paramMatrix.SetRow(0, new Vector4(k1x, k2x, k3x));
            paramMatrix.SetRow(1, new Vector4(k1y, k2y, k3y));
            paramMatrix.SetRow(2, new Vector4(k1z, k2z, k3z));
            
            // k1 = damping.Get(stream) / (math.PI * frequency.Get(stream));
            // k2 = 1f / math.pow(2f * math.PI * frequency.Get(stream), 2f);
            // k3 = response.Get(stream) * damping.Get(stream) / (2 * math.PI * frequency.Get(stream));
            
            return paramMatrix;
        }
    }

    public void ProcessRootMotion(AnimationStream stream)
    {
        
    }

    public FloatProperty jobWeight { get; set; }
}

[Serializable]
public struct SecondOrderTransformData : IAnimationJobData
{
    public NativeReference<Vector3> velocityRef;
    
    public const float saneFrequency = 1f;
    public const float saneDamping = 0.5f;
    public const float saneResponse = 1f; //TODO: check this behaviour
    
    private const float saneMaxPositionDistance = 1f;
    private const float saneMaxRotationAngle = 90f;

    private static readonly Vector3 sanePerAxisRotResponse = new Vector3(saneResponse, saneResponse, saneResponse);
    private static readonly Vector3 sanePerAxisRotFrequency = new Vector3(saneFrequency, saneFrequency, saneFrequency);
    private static readonly Vector3 sanePerAxisRotDamping = new Vector3(saneDamping, saneDamping, saneDamping);
    
    
    
    [Header("REFERENCES:")]
    public Transform constrainedObject;
    [SyncSceneToStream] public Transform sourceObject;
    [SyncSceneToStream] public bool useLocalSpace;
    [SyncSceneToStream, ShowIf("useLocalSpace")] public Transform localSpaceTransform;
    
    
    [Header("CONFIG:")]
    [SyncSceneToStream] public float maxPositionDistance;
    
    
    [Header("PARAMETERS:")]
    [SyncSceneToStream] public float posFrequency;
    [SyncSceneToStream] public float posDamping;
    [SyncSceneToStream] public float posResponse;
    
    [SyncSceneToStream] public float maxRotationAngle;
    [SyncSceneToStream] public float rotFrequency;
    [SyncSceneToStream] public float rotDamping;
    [SyncSceneToStream] public float rotResponse;
    
    [SyncSceneToStream] public Vector3 perAxisRotFrequency;
    [SyncSceneToStream] public Vector3 perAxisRotDamping;
    [SyncSceneToStream] public Vector3 perAxisRotResponse;
    
    
    public bool IsValid() => !(constrainedObject == null || sourceObject == null);

    public void SetDefaultValues()
    {
        constrainedObject = null;
        sourceObject = null;
        
        posFrequency = saneFrequency;
        posDamping = saneDamping;
        posResponse = saneResponse;

        rotFrequency = saneFrequency;
        rotDamping = saneDamping;
        rotResponse = saneResponse;

        perAxisRotFrequency = sanePerAxisRotFrequency;
        perAxisRotDamping = sanePerAxisRotDamping;
        perAxisRotResponse = sanePerAxisRotResponse;
        
        maxPositionDistance = saneMaxPositionDistance;
        maxRotationAngle = saneMaxRotationAngle;
    }
}

public class SecondOrderTransformBinder : AnimationJobBinder<SecondOrderTransformJob, SecondOrderTransformData>
{
    public override SecondOrderTransformJob Create(Animator animator, ref SecondOrderTransformData data, Component component)
    {
        var job = new SecondOrderTransformJob();

        job.impulseVel = data.velocityRef;
        
        job.isPreviewing = !Application.isPlaying;
        
        job.constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject);
        job.source = ReadWriteTransformHandle.Bind(animator, data.sourceObject);
        if(data.localSpaceTransform != null)
            job.localSpaceTransform = ReadWriteTransformHandle.Bind(animator, data.localSpaceTransform);

        job.useLocalSpace = BoolProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.useLocalSpace)));

        job.maxPositionDistance = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.maxPositionDistance)));
        job.maxRotationAngle = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.maxRotationAngle)));
        
        job.frequency = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.posFrequency)));
        job.damping = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.posDamping)));
        job.response = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.posResponse)));

        job.rotFrequency = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.rotFrequency)));
        job.rotDamping = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.rotDamping)));
        job.rotResponse = FloatProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.rotResponse)));
        
        job.perAxisFrequency = Vector3Property.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.perAxisRotFrequency)));
        job.perAxisDamping = Vector3Property.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.perAxisRotDamping)));
        job.perAxisResponse = Vector3Property.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.perAxisRotResponse)));
        
        job.currVel = Vector3.zero;

        if (data.useLocalSpace)
        {
            job.currPos = data.sourceObject.localPosition;
            job.currTargetPos = data.sourceObject.localPosition;
            job.prevTargetPos = data.sourceObject.localPosition;
            job.currRot = data.sourceObject.localRotation;
            job.currTargetRot = data.sourceObject.localRotation;
        }
        else
        {
            job.currPos = data.sourceObject.position;
            job.currTargetPos = data.sourceObject.position;
            job.prevTargetPos = data.sourceObject.position;
            job.currRot = data.sourceObject.rotation;
            job.currTargetRot = data.sourceObject.rotation;
        }

        job.xDyn = new SecondOrderFloat(job.currRot.x);
        job.yDyn = new SecondOrderFloat(job.currRot.y);
        job.zDyn = new SecondOrderFloat(job.currRot.z);
        job.wDyn = new SecondOrderFloat(job.currRot.w);
        
        return job;
    }

    public override void Destroy(SecondOrderTransformJob job)
    {
        
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Second Order Transform")]
public class SecondOrderTransformConstraint : RigConstraint<SecondOrderTransformJob, SecondOrderTransformData, SecondOrderTransformBinder>
{
    
}
