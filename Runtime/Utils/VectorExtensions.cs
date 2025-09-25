using System.Collections.Generic;
using UnityEngine;

public static class VectorExtensions
{
    public static Color WithAlpha(this Color c, float alpha) => new Color(c.r, c.g, c.b, alpha);

    public static Vector4 SmoothDamp(
       Vector4 currValue,
       Vector4 targetValue,
       ref Vector4 velocity,
       float smoothtime
       )
    {
        Vector3 threeVelocity = velocity;
        float oneVelocity = velocity.w;

        Vector3 currXYZ = Vector3.SmoothDamp(currValue, targetValue, ref threeVelocity, smoothtime);
        float currW = Mathf.SmoothDamp(currValue.w, targetValue.w, ref oneVelocity, smoothtime);

        return new Vector4(currXYZ.x, currXYZ.y, currXYZ.z, currW);
    }

    public static Vector3 MultipliedWith(this Vector3 v, Vector3 u)
    {
        v.x *= u.x;
        v.y *= u.y;
        v.z *= u.z;

        return v;
    }

    public static float RemapFromRange(this float value, Vector2 fromRange)
    {
        return Mathf.InverseLerp(fromRange.Min(), fromRange.Max(), value);
    }

    public static float RemapToSignedUnit(this float value) => ToRange(value, new Vector2(-1, 1f));
    
    public static float RemapRange01(this float value) => ToRange(value, new Vector2(0f, 1f));

    public static Vector3 UnitToSignedUnit(this Vector3 vec) => vec * 2f - Vector3.one;

    public static float UnitToSignedUnit(this float value) => value * 2f - 1f;
    
    public static float ToRange(this float value, Vector2 toRange)
    {
        return Mathf.Lerp(toRange.Min(), toRange.Max(), value);
    }

    public static float Min(this Vector2 vector)
    {
        return Mathf.Min(vector.x, vector.y);
    }

    public static float Max(this Vector2 vector)
    {
        return Mathf.Max(vector.x, vector.y);
    }

    //public static float RemapValueTo(this Vector2 fromRange, float value, Vector2 toRange)
    //{
    //	if (toRange.x == toRange.y)
    //		return -1f;


    //	float fromRangeDist = Mathf.Abs(Mathf.Max(fromRange.x, fromRange.y) - Mathf.Min(fromRange.x, fromRange.y));
    //	float clampedValue = Mathf.Clamp(value, Mathf.Min(fromRange.x, fromRange.y), Mathf.Max(fromRange.x, fromRange.y));

    //	return value - Mathf.Min(fromRange.x, fromRange.y)

    //	return -1f;
    //}

    public static Vector3 Average(this List<Vector3> vectors)
    {
        Vector3 avg = Vector3.zero;

        foreach (var vector in vectors)
            avg += vector;

        avg /= (float)vectors.Count;

        return avg;
    }


    public static Vector3 With(this Vector3 original, float? x = null, float? y = null, float? z = null)
    {
        float newX = x.HasValue ? x.Value : original.x;
        float newY = y.HasValue ? y.Value : original.y;
        float newZ = z.HasValue ? z.Value : original.z;

        return new Vector3(newX, newY, newZ);
    }

    public static Vector2 With(this Vector2 original, float? x = null, float? y = null)
    {
        float newX = x.HasValue ? x.Value : original.x;
        float newY = y.HasValue ? y.Value : original.y;

        return new Vector2(newX, newY);
    }

    public static Color With(this Color original, float? r = null, float? g = null, float? b = null, float? a = null)
    {
        float newR = r.HasValue ? r.Value : original.r;
        float newG = g.HasValue ? g.Value : original.g;
        float newB = b.HasValue ? b.Value : original.b;
        float newA = a.HasValue ? a.Value : original.a;

        return new Color(newR, newG, newB, newA);
    }

    public static Vector3 FlatInXZ(this Vector2 original)
    {
        return new Vector3(original.x, 0f, original.y);
    }

    public static Vector3 FlatInXZ(this Vector3 original)
    {
        return original.With(y: 0f);
    }

    public static Vector3 To(this Vector3 fromPos, Vector3 toPos)
    {
        return (toPos - fromPos);
    }

    public static Vector2 To(this Vector2 fromPos, Vector2 toPos)
    {
        return (toPos - fromPos);
    }

    public static Vector3 FlatTo(this Vector3 fromPos, Vector3 toPos)
    {
        return (toPos.FlatInXZ() - fromPos.FlatInXZ());
    }

    public static Vector3 PlaneProj(this Vector3 vect, Vector3 normal)
    {
        return vect - Vector3.Dot(vect, normal) / normal.sqrMagnitude * normal;
    }

    public static Vector2 Slerp(this Vector2 current, Vector2 target, float t)
    {
        Vector3 tempCurrent = new Vector3(current.x, current.y, 0f);
        Vector3 tempTarget = new Vector3(target.x, target.y, 0f);
        Vector3 tempResult = Vector3.Slerp(tempCurrent, tempTarget, t);
        return new Vector2(tempResult.x, tempResult.y);
    }

    public static bool ApproxEquals(this Vector3 vecA, Vector3 vecB)
    {
        return (
            Mathf.Approximately(vecA.x, vecB.x)
            && Mathf.Approximately(vecA.y, vecB.y)
            && Mathf.Approximately(vecA.z, vecB.z)
            );
    }

    public static bool Contains(this Collider col, Vector3 point)
    {
        return point.ApproxEquals(col.ClosestPoint(point));
    }

    public static float Max(this Vector3 vec)
    {
        return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
    }



    //public static void DrawSquare(
    //	Vector3 p0, 
    //	Vector3 p1, 
    //	Vector3 p2, 
    //	Vector3 p3, 
    //	Color squareColor
    //	)
    //{
    //	var color = Gizmos.color;
    //	Gizmos.color = squareColor;
    //	Gizmos.DrawLine(p0, p1);
    //	Gizmos.DrawLine(p1, p2);
    //	Gizmos.DrawLine(p2, p3);
    //	Gizmos.DrawLine(p3, p0);
    //	Gizmos.color = color;
    //}

    //public static void DrawDebugSquare(
    //	Vector3 p0, 
    //	Vector3 p1, 
    //	Vector3 p2, 
    //	Vector3 p3, 
    //	Color squareColor
    //	)
    //{
    //	Debug.DrawLine(p0, p1, squareColor);
    //	Debug.DrawLine(p1, p2, squareColor);
    //	Debug.DrawLine(p2, p3, squareColor);
    //	Debug.DrawLine(p3, p0, squareColor);
    //}

    //public static void DrawCross(
    //	this Vector2 pos,
    //	Vector3 up,
    //	Vector3 right,
    //	float size,
    //	Color crossColor
    //	)
    //{
    //	Vector3 newPos = pos;
    //	newPos.DrawThatCross(up, right, size, crossColor);
    //}

    //public static void DrawThatCross(
    //	this Vector3 pos,
    //	Vector3 up,
    //	Vector3 right,
    //	float size,
    //	Color crossColor
    //	)
    //{
    //	Vector3 S, N, W, E;

    //	S = pos - up * size;
    //	N = pos + up * size;
    //	W = pos - right * size;
    //	E = pos + right * size;

    //	var color = Gizmos.color;
    //	Gizmos.color = crossColor;
    //	Gizmos.DrawLine(S, N);
    //	Gizmos.DrawLine(W, E);
    //	Gizmos.color = color;
    //}
}



