using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Draw
{
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