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
    //... OFFSETS:
    public Vector3 translationOffset;
    public Quaternion rotationalOffset;
    
    //... IMPULSE:
    public NativeReference<Vector3> impulseVel;
    public NativeReference<Vector3> alignedImpulseVel;
    public NativeReference<Vector3> impulseTorque;
    
    const float k_FixedDt = 0.01666667f;

    public ReadWriteTransformHandle constrained;
    public ReadWriteTransformHandle source;
    
    public bool isPreviewing;

    public BoolProperty useLocalSpace;
    public ReadWriteTransformHandle localSpaceTransform;

    public BoolProperty constrainPosition;
    public BoolProperty constrainRotation;
    
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
    // public Vector3Property perAxisFrequency;
    // public Vector3Property perAxisDamping;
    // public Vector3Property perAxisResponse;

    
    public void ProcessAnimation(AnimationStream stream)
    {
        bool constrainingPosition = constrainPosition.Get(stream);
        bool constrainingRotation = constrainRotation.Get(stream);
        bool usingLocalSpace = this.useLocalSpace.Get(stream) && localSpaceTransform.IsValid(stream);

        var targetPos = source.GetPosition(stream);
        
        float w = jobWeight.Get(stream);

        if (isPreviewing)
        {
            if (constrainingPosition)
            {
                if(!usingLocalSpace)
                    constrained.SetPosition(stream, source.GetPosition(stream));
                else
                {
                    // source.GetGlobalTR(stream, out Vector3 srcPos, out Quaternion srcRot);
                    // var sourceTx = new AffineTransform(srcPos, srcRot);
                    // var offsetTx = new AffineTransform(translationOffset, rotationalOffset);
                    // sourceTx *= offsetTx;
                    //
                    // constrained.GetGlobalTR(stream, out Vector3 conPos, out Quaternion conRot);
                    
                    
                    localSpaceTransform.GetGlobalTR(stream, out var localTxPos, out var localTxRot);
                    var localTx = new AffineTransform(localTxPos, localTxRot);
                    var tempTargetPos = localTx.InverseTransform(targetPos);
                    constrained.SetLocalPosition(stream, tempTargetPos);
                    
                    // constrained.SetLocalPosition(stream, Vector3.Lerp(currPos, tempTargetPos, 1f - w));
                    // constrained.SetLocalPosition(stream, source.GetLocalPosition(stream));
                }
            }

            if (constrainingRotation)
            {
                if(!usingLocalSpace)
                    constrained.SetRotation(stream, source.GetRotation(stream));
                else
                    constrained.SetLocalRotation(stream, source.GetLocalRotation(stream));
            }
            
            AnimationRuntimeUtils.PassThrough(stream, constrained);
            
            return;
        }
        
        // if (isPreviewing)
        // {
        //     if (constrainingPosition)
        //     {
        //         if(!useLocalSpace)
        //             constrained.SetPosition(stream, source.GetPosition(stream));
        //         else
        //         {
        //             constrained.SetLocalPosition(stream, source.GetLocalPosition(stream));
        //             
        //             // localSpaceTransform.GetGlobalTR(stream, out var localTxPos, out var localTxRot);
        //             // var localTransformMatrix = new AffineTransform(localTxPos, localTxRot);
        //             // var tempTargetPos = localTransformMatrix.InverseTransform(targetPos);
        //             // constrained.SetLocalPosition(stream, Vector3.Lerp(currPos, tempTargetPos, 1f - w));
        //         }
        //     }
        //
        //     if (constrainingRotation)
        //     {
        //         if(!useLocalSpace)
        //             constrained.SetRotation(stream, source.GetRotation(stream));
        //         else
        //             constrained.SetLocalRotation(stream, source.GetLocalRotation(stream));
        //     }
        //     
        //     // AnimationRuntimeUtils.PassThrough(stream, constrained);
        //     
        //     return;
        // }

        float streamDt = Mathf.Abs(stream.deltaTime);

        if (streamDt <= 0f)
            return;

        // if(w <= 0f || streamDt <= 0f)
        //     return;

        var posDamping = damping.Get(stream);
        var posFrequency = frequency.Get(stream);
        var posResponse = response.Get(stream);
        
        k1 = damping.Get(stream) / (math.PI * frequency.Get(stream));
        k2 = 1f / math.pow(2f * math.PI * frequency.Get(stream), 2f);
        k3 = response.Get(stream) * damping.Get(stream) / (2 * math.PI * frequency.Get(stream));
        
        rotK1 = rotDamping.Get(stream) / (math.PI * rotFrequency.Get(stream));
        rotK2 = 1f / math.pow(2f * math.PI * rotFrequency.Get(stream), 2f);
        rotK3 = rotResponse.Get(stream) * rotDamping.Get(stream) / (2 * math.PI * rotFrequency.Get(stream));
        

        // prevTargetRot = currTargetRot;
        if (usingLocalSpace)
        {
            localSpaceTransform.GetGlobalTR(stream, out var localTxPos, out var localTxRot);
            var localTransformMatrix = new AffineTransform(localTxPos, localTxRot);
            targetPos = localTransformMatrix.InverseTransform(targetPos);
        }
        
        prevTargetPos = currTargetPos;
        currTargetPos = targetPos;
        
        currTargetRot = source.GetRotation(stream);
        if (usingLocalSpace)
            currTargetRot = Quaternion.Inverse(localSpaceTransform.GetRotation(stream)) * currTargetRot;

        
        var effectiveDt = Mathf.Abs(streamDt);

        //... integrate rotation:
        if (impulseTorque.IsCreated)
        {
            Quaternion impulseQuat = new Quaternion(impulseTorque.Value.x, impulseTorque.Value.y, impulseTorque.Value.z, 0);
            Quaternion deltaVel =MultiplyQuaternion(0.5f, currRot * impulseQuat);
            // modify the quaternion velocity
            yDyn.Impulse(deltaVel.y);
            xDyn.Impulse(deltaVel.x);
            zDyn.Impulse(deltaVel.z);
            wDyn.Impulse(deltaVel.w);
            impulseTorque.Value = Vector3.zero;
        }
        
        Quaternion MultiplyQuaternion(float scalar, Quaternion q)
        {
            return new Quaternion(q.x * scalar, q.y * scalar, q.z * scalar, q.w * scalar);
        }
        
        currRot.y = yDyn.Tick(effectiveDt, currTargetRot.y, rotK1, rotK2, rotK3);
        currRot.x = xDyn.Tick(effectiveDt, currTargetRot.x, rotK1, rotK2, rotK3);
        currRot.z = zDyn.Tick(effectiveDt, currTargetRot.z, rotK1, rotK2, rotK3);
        currRot.w = wDyn.Tick(effectiveDt, currTargetRot.w, rotK1, rotK2, rotK3);
        
        currRot.Normalize();

        //... integrate position:
        if (impulseVel.IsCreated)
        {
            currVel += impulseVel.Value;
            impulseVel.Value = Vector3.zero;
        }

        if (alignedImpulseVel.IsCreated)
        {
            currVel += currRot * alignedImpulseVel.Value;
            alignedImpulseVel.Value = Vector3.zero;
        }
        
        var xDeriv = (currTargetPos - prevTargetPos) / effectiveDt;
        currPos += effectiveDt * currVel;
        currVel += effectiveDt * (currTargetPos + k3 * xDeriv - currPos - k1 * currVel) / k2;

        
        //.. TODO: 
        // while (streamDt > 0f)
        // {
        //  run a tick with at most a 1/60 dt, until you're caught up.
            // streamDt -= k_FixedDt;
        // }

        if (usingLocalSpace)
        {
            if(constrainingRotation)
                constrained.SetLocalRotation(stream, Quaternion.Slerp(currRot, currTargetRot, 1f - w));
            
            if(constrainingPosition)
                constrained.SetLocalPosition(stream, Vector3.Lerp(currPos, currTargetPos, 1f - w));
        }
        else
        {
            if(constrainingRotation)
                constrained.SetRotation(stream, Quaternion.Slerp(currRot, currTargetRot, 1f - w));
            
            if(constrainingPosition)
                constrained.SetPosition(stream, Vector3.Lerp(currPos, currTargetPos, 1f - w));
            
            // constrained.SetRotation(stream, currRot);
            // constrained.SetPosition(stream, currPos);
        }
        
        // Debug.LogWarning($"currTargPos: ({currTargetPos.x}, {currTargetPos.y}, {currTargetPos.z})");

       
    }

    public void ProcessRootMotion(AnimationStream stream){}

    public FloatProperty jobWeight { get; set; }
}

[Serializable]
public struct SecondOrderTransformData : IAnimationJobData
{
    public NativeReference<Vector3> velocityRef;
    public NativeReference<Vector3> torqueRef;
    public NativeReference<Vector3> alignedImpulseRef;

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
    [SyncSceneToStream] public bool constrainPosition;
    [SyncSceneToStream] public bool constrainRotation;
    [SyncSceneToStream] public float maxPositionDistance;
    
    
    [Header("PARAMETERS:")]
    [SyncSceneToStream] public float posFrequency;
    [SyncSceneToStream] public float posDamping;
    [SyncSceneToStream] public float posResponse;
    
    [SyncSceneToStream] public float maxRotationAngle;
    [SyncSceneToStream] public float rotFrequency;
    [SyncSceneToStream] public float rotDamping;
    [SyncSceneToStream] public float rotResponse;
    
    
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

        maxPositionDistance = saneMaxPositionDistance;
        maxRotationAngle = saneMaxRotationAngle;
        
        constrainPosition = true;
        constrainRotation = true;
    }
}

public class SecondOrderTransformBinder : AnimationJobBinder<SecondOrderTransformJob, SecondOrderTransformData>
{
    public override SecondOrderTransformJob Create(Animator animator, ref SecondOrderTransformData data, Component component)
    {
        var job = new SecondOrderTransformJob();

        var sourceTransform = data.sourceObject.transform;
        var constrainedTransform = data.constrainedObject.transform;
        
        var sourceTx = new AffineTransform(sourceTransform.position, sourceTransform.rotation);
        var constrainedTx = new AffineTransform(constrainedTransform.position, constrainedTransform.rotation);
        var tmp = sourceTx.InverseMul(constrainedTx);
        
        job.translationOffset = tmp.translation;
        job.rotationalOffset = tmp.rotation;
        
        //.. FROM MULTIPARENT, TRY TO ADD IN OFFSET OPTION..?
        // var drivenTx = new AffineTransform(data.constrainedObject.position, data.constrainedObject.rotation);
        // for (int i = 0; i < sourceObjects.Count; ++i)
        // {
        //     var sourceTransform = sourceObjects[i].transform;
        //
        //     var srcTx = new AffineTransform(sourceTransform.position, sourceTransform.rotation);
        //     var srcOffset = AffineTransform.identity;
        //     var tmp = srcTx.InverseMul(drivenTx);
        //
        //     if (data.maintainPositionOffset)
        //         srcOffset.translation = tmp.translation;
        //     if (data.maintainRotationOffset)
        //         srcOffset.rotation = tmp.rotation;
        //
        //     job.sourceOffsets[i] = srcOffset;
        // }
        
        job.impulseVel = data.velocityRef;
        job.impulseTorque = data.torqueRef;
        job.alignedImpulseVel = data.alignedImpulseRef;

        job.constrainPosition = BoolProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.constrainPosition)));
        job.constrainRotation = BoolProperty.Bind(animator, component, ConstraintsUtils.ConstructConstraintDataPropertyName(nameof(data.constrainRotation)));
        
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
        if(job.impulseVel.IsCreated)
            job.impulseVel.Dispose();
        
        if(job.impulseVel.IsCreated)
            job.alignedImpulseVel.Dispose();
        
        if(job.impulseTorque.IsCreated)
            job.impulseTorque.Dispose();
    }
}

[DisallowMultipleComponent, AddComponentMenu("Animation Rigging/Second Order Transform")]
public class SecondOrderTransformConstraint : RigConstraint<SecondOrderTransformJob, SecondOrderTransformData, SecondOrderTransformBinder>
{
    
}

// Matrix4x4 GetPerAxisParams(Vector3 freq, Vector3 damp, Vector3 resp)
// {
//     Vector3 perAxisParams = Vector3.zero;
//     Matrix4x4 paramMatrix = new Matrix4x4();
//
//     var k1x = damp.x / (math.PI * freq.x);
//     var k1y = damp.y / (math.PI * freq.y);
//     var k1z = damp.z / (math.PI * freq.z);
//
//     var k2x = 1f / math.pow(2f * math.PI * freq.x, 2f);
//     var k2y = 1f / math.pow(2f * math.PI * freq.y, 2f);
//     var k2z = 1f / math.pow(2f * math.PI * freq.z, 2f);
//
//     var k3x = resp.x * damp.x / (2f * math.PI * freq.x);
//     var k3y = resp.y * damp.y / (2f * math.PI * freq.y);
//     var k3z = resp.z * damp.z / (2f * math.PI * freq.z);
//
//     paramMatrix.SetRow(0, new Vector4(k1x, k2x, k3x));
//     paramMatrix.SetRow(1, new Vector4(k1y, k2y, k3y));
//     paramMatrix.SetRow(2, new Vector4(k1z, k2z, k3z));
//             
//     return paramMatrix;
// }
