using System;
using UnityEngine;

[SelectionBase]
public class PhysicsFlicker : MonoBehaviour
{
    public LayerMask mask;
    public ForceMode forceMode = ForceMode.Impulse;
    public QueryTriggerInteraction queryMode;
    public bool logDebug;
    public bool drawDebug;
    public float flickForce = 10f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            var foundRbProvider = GetComponent<IRigidbodyProvider>();
            if(foundRbProvider != null)
                rb = foundRbProvider.Rigidbody;
        }
    }

    public void OnDrawGizmos()
    {
        if (!Application.isPlaying || !drawDebug || rb == null)
            return;

        using (new ColorContext(Color.green))
        {
            Gizmos.DrawWireSphere(rb.worldCenterOfMass, 1f);
        }
    }
}