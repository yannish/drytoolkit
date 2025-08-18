using UnityEngine;

[CreateAssetMenu(menuName = "RefVariable/FloatVariable_NEW", fileName = "floatVariable")]
public class FloatVar : ScriptableObject
{
#if UNITY_EDITOR
    [Multiline]
    public string DeveloperDescription = "";
#endif
    public float value;
}
