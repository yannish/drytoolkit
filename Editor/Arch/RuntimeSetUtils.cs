using System;
using UnityEngine;
using UnityEditor;

public class RuntimeSetUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Cleanup()
    {
        Debug.LogWarning("Registering subsystem cleanup for runtime sets.");
        EditorApplication.playModeStateChanged -= ClearRuntimeSets;
        EditorApplication.playModeStateChanged += ClearRuntimeSets;
    }

    public static void ClearRuntimeSets(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredEditMode:
                CleanUpRuntimeSets();
                break;
            case PlayModeStateChange.ExitingEditMode:
                CleanUpRuntimeSets();
                break;
            case PlayModeStateChange.EnteredPlayMode:
                break;
            case PlayModeStateChange.ExitingPlayMode:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        // EditorApplication.delayCall += () =>
        // {
        //     Debug.LogWarning("Cleaning up runtime sets;");
        //
        //     CleanUpRuntimeSets();
        // };

        void CleanUpRuntimeSets()
        {
            var allRuntimeSetTypes = TypeCache.GetTypesDerivedFrom(typeof(RuntimeSet<>));
            foreach (var runtimeSetType in allRuntimeSetTypes)
            {
                var sets = Resources.LoadAll("RuntimeSets", runtimeSetType);
                foreach (var set in sets)
                {
                    if (set is ScriptableObject scriptableSet)
                    {
                        var clearMethod = runtimeSetType.GetMethod("ClearSet");
                        clearMethod?.Invoke(scriptableSet, null); // Call ClearSet dynamically
                    }
                }
            }
        }
    }
}
