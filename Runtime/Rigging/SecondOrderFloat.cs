using UnityEngine;

public struct SecondOrderFloat
{
    private float prevTarget;
    private float currValue;
    private float currVel;

    public SecondOrderFloat(float x0)
    {
        prevTarget = x0;
        currValue = x0;
        currVel = 0f;
    }

    public void Flick(float flick) => currVel += flick;

    public float Tick(
        float dt, 
        float target,
        float k1,
        float k2,
        float k3,
        float? targetVel = null
    )
    {
        if (targetVel == null)
        {
            targetVel = (target - prevTarget) / dt;
            prevTarget = target;
        }

        currValue += dt * currVel;
        currVel += dt * (target + k3 * targetVel.Value - currValue - k1 * currVel) / k2;
            
        return currValue;
    }
}