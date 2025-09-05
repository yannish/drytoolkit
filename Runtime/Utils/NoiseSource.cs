using UnityEngine;
using System;

public class NoiseChannel
{
    public NoiseChannel(Vector2 newSampleDir)
    {
        sampleDir = newSampleDir;
    }

    public float currSample;
    public Vector2 currSamplePos;
    public Vector2 sampleDir;

    private const float rangeSize = 10000f;

    public void TickNoise(float sampleSpeed) => currSamplePos += Time.deltaTime * sampleSpeed * sampleDir;

    public float SampleNoise(Vector3 offset) => Mathf.PerlinNoise(currSamplePos.x + offset.x, currSamplePos.y + offset.z);

    internal void Init()
    {
        currSamplePos = new Vector2(
            UnityEngine.Random.Range(-rangeSize, rangeSize),
            UnityEngine.Random.Range(-rangeSize, rangeSize)
        );
    }
}


[Serializable]
public class NoiseSource
{
    public float sampleSpeed = 60f;

    public NoiseChannel firstChannel = new NoiseChannel(new Vector2(0.2f, 0.7f));
    public NoiseChannel secondChannel = new NoiseChannel(new Vector2(0.7f, 0.2f));
    public NoiseChannel thirdChannel = new NoiseChannel(new Vector2(0.7f, 0.7f));

    public void Initialize()
    {
        firstChannel.Init();
        secondChannel.Init();
        thirdChannel.Init();
    }


    public void TickNoise()
    {
        firstChannel.TickNoise(sampleSpeed);
        secondChannel.TickNoise(sampleSpeed);
        thirdChannel.TickNoise(sampleSpeed);
    }

    public float SampleNoise(Vector3 pos = default)
    {
        return (firstChannel.SampleNoise(pos) + secondChannel.SampleNoise(pos)) * 0.5f;
    }

    public Vector3 Sample3ChannelNoise(Vector3 pos = default)
    {
        return new Vector3(
            firstChannel.SampleNoise(pos),
            thirdChannel.SampleNoise(pos),
            secondChannel.SampleNoise(pos)
        );
    }
}