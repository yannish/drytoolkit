using UnityEngine;
using UnityEngine.Animations;

public class SimpleClipHandle
{
    public AnimationClip clip;
    public AnimationClipPlayable clipPlayable;
    
    //... state stuff:
    public float blendVel = 0f;
    public float targetWeight = 0f;
    public float currWeight = 0f;
}
