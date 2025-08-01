using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AnimationSystem/ClipSet", fileName = "Clip Set")]
public class ClipSet : ScriptableObject
{
    public List<AnimationClip> clips;
}
