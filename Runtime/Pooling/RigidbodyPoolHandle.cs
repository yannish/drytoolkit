using System;
using drytoolkit.Runtime.Pooling;
using UnityEngine;

public class RigidbodyPoolHandle : MonoBehaviour, IPoolable
{
    [SerializeField] Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public void OnGetFromPool(Vector3 position, Quaternion rotation)
    {
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.Move(position, rotation);
    }

    public void OnReturnToPool()
    {
        // rb.linearVelocity = Vector3.zero;
        // rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}
