using UnityEngine;
using UnityEditor;

public class RuntimeSetUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Cleanup()
    {
        EditorApplication.playModeStateChanged -= ClearRuntimeSets;
        EditorApplication.playModeStateChanged += ClearRuntimeSets;
    }

    public static void ClearRuntimeSets(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode)
            return;
        
        Debug.LogWarning("Cleaning up runtime sets;");

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
