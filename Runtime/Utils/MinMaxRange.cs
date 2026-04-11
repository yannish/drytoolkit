using UnityEngine;

[System.Serializable]
public class MinMaxRange
{
    public float min, max;

    public MinMaxRange(float min, float max) {
        this.min = min;
        this.max = max;
    }

    public float Lerp(float t)
    {
        return Mathf.Lerp(min, max, t);
    }

    public float Randomize()
    {
        return Random.Range(min, max);
    }
}