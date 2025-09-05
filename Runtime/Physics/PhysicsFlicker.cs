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

    public Rigidbody rb;

    public void OnDrawGizmos()
    {
        if (rb == null)
            return;

        if (drawDebug)
            Gizmos.DrawWireSphere(rb.worldCenterOfMass, 1f);
    }
}