using drytoolkit.Runtime.Animation;
using UnityEngine;
using UnityEngine.Serialization;

public class AnimationSystemState : MonoBehaviour
{
    [FormerlySerializedAs("ClipEventConfig")]
    [Expandable]
    public ClipEvent clipEvent;
}
