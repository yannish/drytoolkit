using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "AnimationSystem/ClipSet", fileName = "Clip Set")]
public class ClipSet : ScriptableObject
{
    public List<ClipData> clipDataList = new List<ClipData>();
    public float randomFloat;

    public static event Action<ClipSet> OnAnyValidate;
    
    private void OnValidate()
    {
        Debug.Log("OnValidate in clipset");
        OnAnyValidate?.Invoke(this);
    }
    
    public Action onValidate;
}

[Serializable]
public class ClipData
{
    public AnimationClip clip;
    public bool mute;
}
