using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClipComparer))]
public class ClipComparerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DrawControls();
    }

    private void DrawControls()
    {
        var comparer = (ClipComparer)target;
        if (comparer.clipA == null || comparer.clipB == null)
            return;
        
        if(GUILayout.Button("COMPARE"))
            CompareClips(comparer.clipA, comparer.clipB);
    }
    
    void CompareClips(AnimationClip a, AnimationClip b)
    {
        var bindingsA = AnimationUtility.GetCurveBindings(a);
        var bindingsB = AnimationUtility.GetCurveBindings(b);

        var objBindingsA = AnimationUtility.GetObjectReferenceCurveBindings(a);
        var objBindingsB = AnimationUtility.GetObjectReferenceCurveBindings(b);

        var setA = new HashSet<EditorCurveBinding>(bindingsA);
        var setB = new HashSet<EditorCurveBinding>(bindingsB);

        Debug.Log("=== Curve Bindings Differences ===");
        foreach (var onlyInA in setA.Except(setB))
            Debug.Log($"Only in A: {onlyInA.propertyName} on {onlyInA.path}");

        foreach (var onlyInB in setB.Except(setA))
            Debug.Log($"Only in B: {onlyInB.propertyName} on {onlyInB.path}");

        Debug.Log("=== Changed Curves ===");
        foreach (var binding in setA.Intersect(setB))
        {
            var curveA = AnimationUtility.GetEditorCurve(a, binding);
            var curveB = AnimationUtility.GetEditorCurve(b, binding);

            if (!AreCurvesEqual(curveA, curveB))
            {
                Debug.Log($"Changed: {binding.propertyName} on {binding.path}");
            }
        }

        Debug.Log("=== Object Reference Bindings Differences ===");
        CompareObjectReferenceCurves(a, b, objBindingsA, objBindingsB);
    }

    void CompareObjectReferenceCurves(AnimationClip a, AnimationClip b, EditorCurveBinding[] bindingsA, EditorCurveBinding[] bindingsB)
    {
        var setA = new HashSet<EditorCurveBinding>(bindingsA);
        var setB = new HashSet<EditorCurveBinding>(bindingsB);

        foreach (var onlyInA in setA.Except(setB))
            Debug.Log($"[Obj] Only in A: {onlyInA.propertyName} on {onlyInA.path}");

        foreach (var onlyInB in setB.Except(setA))
            Debug.Log($"[Obj] Only in B: {onlyInB.propertyName} on {onlyInB.path}");

        foreach (var binding in setA.Intersect(setB))
        {
            var curveA = AnimationUtility.GetObjectReferenceCurve(a, binding);
            var curveB = AnimationUtility.GetObjectReferenceCurve(b, binding);

            if (!AreObjectReferenceCurvesEqual(curveA, curveB))
            {
                Debug.Log($"[Obj] Changed: {binding.propertyName} on {binding.path}");
            }
        }
    }

    bool AreCurvesEqual(AnimationCurve a, AnimationCurve b)
    {
        if (a == null || b == null) return a == b;
        if (a.keys.Length != b.keys.Length) return false;

        for (int i = 0; i < a.keys.Length; i++)
        {
            if (a.keys[i].time != b.keys[i].time ||
                a.keys[i].value != b.keys[i].value ||
                a.keys[i].inTangent != b.keys[i].inTangent ||
                a.keys[i].outTangent != b.keys[i].outTangent)
            {
                return false;
            }
        }
        return true;
    }

    bool AreObjectReferenceCurvesEqual(ObjectReferenceKeyframe[] a, ObjectReferenceKeyframe[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].time != b[i].time || a[i].value != b[i].value)
                return false;
        }
        return true;
    }
}
