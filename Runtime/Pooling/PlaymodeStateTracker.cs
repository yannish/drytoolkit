using UnityEditor;
using UnityEngine;

public class PlaymodeStateTracker
{
    public static bool IsExitingPlayMode { get; private set; }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                IsExitingPlayMode = true;
            else if (state == PlayModeStateChange.EnteredPlayMode)
                IsExitingPlayMode = false;
        };
    }
#endif
}
