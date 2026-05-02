using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Draw
{
    // Draws a circle made of alternating drawn/skipped segments.
    // dashLength controls the arc length (in world units) of each dash and each gap.
    public static void DashedCircle(Vector3 center, Vector3 normal, float radius, float dashLength = 0.1f)
    {
#if UNITY_EDITOR
        if (radius <= 0f || dashLength <= 0f) return;

        float circumference = 2f * Mathf.PI * radius;
        int pairCount = Mathf.Max(1, Mathf.RoundToInt(circumference / (2f * dashLength)));
        float anglePerDash = 360f / pairCount * 0.5f;

        var up   = Mathf.Abs(Vector3.Dot(normal.normalized, Vector3.up)) < 0.99f ? Vector3.up : Vector3.forward;
        var from = Vector3.Cross(normal, up).normalized;

        for (int i = 0; i < pairCount; i++)
        {
            var dashFrom = Quaternion.AngleAxis(i * anglePerDash * 2f, normal) * from;
            Handles.DrawWireArc(center, normal, dashFrom, anglePerDash, radius);
        }
#endif
    }

    // Billboard variant — normal is derived from the scene view camera so the circle faces the viewer.
    public static void DashedCircle(Vector3 center, float radius, float dashLength = 0.1f)
    {
#if UNITY_EDITOR
        if (Camera.current != null)
            DashedCircle(center, Camera.current.transform.forward, radius, dashLength);
#endif
    }

    public static void Cross(Vector3 center, Vector3 normal, Vector3 north, float armLength, float centerSpacing)
    {
#if UNITY_EDITOR
        var east = Vector3.Cross(normal, north);

        void DrawDash(Vector3 dir)
        {
            Vector3 p0 = center + dir * centerSpacing;
            Vector3 p1 = center + dir * (centerSpacing + armLength);
            Handles.DrawLine(p0, p1, 0f);
        }

        DrawDash(north);
        DrawDash(-north);
        DrawDash(east);
        DrawDash(-east);
#endif
    }
}