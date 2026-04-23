using UnityEngine;

[System.Serializable]
public class MinMaxRange
{
    public float min, max;

    public MinMaxRange(float min, float max) 
    {
        this.min = min;
        this.max = max;
    }

    public float Lerp(float t) => Mathf.Lerp(min, max, t);

    public float InverseLerp(float t) => Mathf.InverseLerp(min, max, t);

    public float Randomize() => Random.Range(min, max);

    public float Evaluate(float t)
    {
        if (t < min)
            return min;

        if (t < max)
            return Mathf.Lerp(min, max, t);

        return max;
    }
}