using UnityEngine;

[CreateAssetMenu(menuName = "RefVariable/FloatVar", fileName = "floatVar")]
public class FloatVar : ScriptableObject
{
#if UNITY_EDITOR
    [Multiline]
    public string DeveloperDescription = "";
#endif
    public float value;
}
