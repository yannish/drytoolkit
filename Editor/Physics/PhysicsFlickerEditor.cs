using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhysicsFlicker))]
public class PhysicsFlickerInspector : Editor
{

    private void OnEnable() => SceneView.duringSceneGui += DrawInScene;

    private void OnDisable() => SceneView.duringSceneGui -= DrawInScene;

    void DrawInScene(SceneView view)
    {
		Ray quickRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		Plane plane = new Plane(Vector3.up, Vector3.zero);
		if (plane.Raycast(quickRay, out float enter))
		{
			var planePoint = quickRay.GetPoint(enter);
			Handles.DrawWireDisc(planePoint, Vector3.up, 0.5f);
			Handles.DrawLine(planePoint, planePoint + Vector3.up);
		}


        if (!Application.isPlaying)
            return;

        PhysicsFlicker physFlicker = target as PhysicsFlicker;
        if (physFlicker == null)
            return;

        if (!physFlicker.enabled)
            return;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    Mathf.Infinity,
                    physFlicker.mask,
                    physFlicker.queryMode
                    ))
            {
                var hitPhysFlicker = hit.collider.gameObject.GetComponentInParent<PhysicsFlicker>();
                if (hitPhysFlicker != null && hitPhysFlicker == physFlicker)
                {
                    Rigidbody rb = hit.collider.gameObject.GetComponentInParent<Rigidbody>();
                    if (rb != null)
                        rb.AddForceAtPosition(
                            ray.direction.normalized * hitPhysFlicker.flickForce,
                            hit.point,
                            ForceMode.Acceleration
                            );
                }
            }
        }
    }
}
