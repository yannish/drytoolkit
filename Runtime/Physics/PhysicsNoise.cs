using UnityEngine;

public class PhysicsNoise : MonoBehaviour
{
    [Header("TORQUE:")]
    	public NoiseSource torqueNoiseSource;
    	public float torque;
    	public Vector3 torqueMultiplier;
    	public Vector3 minTorqueNoiseSample;
    	public Vector3 torqueNoiseSample;
    	public Vector3 torqueToApply;
    
    	[Header("FORCE:")]
    	public NoiseSource forceNoiseSource;
    	public NoiseSource forceDirNoiseSource;
    	public Vector3 forceAxisMultiplier = Vector3.one;
    
    	public float force = 1;
    	public float minForceNoiseSample = 0.2f;
    
    	Rigidbody rb;
    
    	private void Awake()
    	{
    		rb = GetComponent<Rigidbody>();
    
    		torqueNoiseSource.Initialize();
    		forceDirNoiseSource.Initialize();
    		forceNoiseSource.Initialize();
    	}
    
    	private void FixedUpdate()
    	{
    		if (rb == null)
    			return;
    
    		torqueNoiseSource.TickNoise();
    		torqueNoiseSample = torqueNoiseSource.Sample3ChannelNoise() * 2f - Vector3.one;
    		var torqueNoiseSampleRemapped = torqueNoiseSample * 2f - Vector3.one;
    		torqueToApply = new Vector3(
    			torque * torqueMultiplier.x * torqueNoiseSampleRemapped.x * Mathf.Ceil(torqueNoiseSample.x - minTorqueNoiseSample.x),
    			torque * torqueMultiplier.y * torqueNoiseSampleRemapped.y * Mathf.Ceil(torqueNoiseSample.y - minTorqueNoiseSample.y),
    			torque * torqueMultiplier.z * torqueNoiseSampleRemapped.z * Mathf.Ceil(torqueNoiseSample.z - minTorqueNoiseSample.z)
    		);
    
    		rb.AddTorque(torqueToApply, ForceMode.Acceleration);
    
    
    		forceNoiseSource.TickNoise();
    		forceDirNoiseSource.TickNoise();
    
    		//.. direction:
    		var forceDirNoiseSample = forceDirNoiseSource.Sample3ChannelNoise() * 2f - Vector3.one;
    		forceDirNoiseSample = Vector3.Scale(forceDirNoiseSample, forceAxisMultiplier);
    		var forceNoiseDir = Quaternion.Euler(forceDirNoiseSample * 180f) * Vector3.forward;
    		
    		//... strength:
    		var forceNoiseSample = forceNoiseSource.Sample3ChannelNoise();
    		var forceStrength = forceNoiseSample.x * Mathf.Ceil(forceNoiseSample.x - minForceNoiseSample) * force;
    		var forceToApply = forceNoiseDir * forceStrength;
    
    		rb.AddForce(forceToApply, ForceMode.Acceleration);
    	}
}
