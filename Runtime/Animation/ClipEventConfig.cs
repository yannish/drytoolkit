using UnityEngine;

[CreateAssetMenu(menuName = "AnimationSystem/ClipEventConfig")]
public class ClipEventConfig : ScriptableObject
{
    public AnimationClip clip;
    
    [Range(0f, 1f)]
    public float eventTime;

    // public float previewTime;
}
