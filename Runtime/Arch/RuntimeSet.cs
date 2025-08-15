using System.Collections.Generic;
using JetBrains.Annotations;

using UnityEngine;
// using UnityEditor;
//
// public class RuntimeSetUtils
// {
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//     public static void Cleanup()
//     {
//         EditorApplication.playModeStateChanged -= ClearRuntimeSets;
//         EditorApplication.playModeStateChanged += ClearRuntimeSets;
//     }
//
//     public static void ClearRuntimeSets(PlayModeStateChange state)
//     {
//         if (state != PlayModeStateChange.ExitingPlayMode)
//             return;
//         
//         Debug.LogWarning("Cleaning up runtime sets;");
//
//         var allRuntimeSetTypes = TypeCache.GetTypesDerivedFrom(typeof(RuntimeSet<>));
//         foreach (var runtimeSetType in allRuntimeSetTypes)
//         {
//             var sets = Resources.LoadAll("RuntimeSets", runtimeSetType);
//             foreach (var set in sets)
//             {
//                 if (set is ScriptableObject scriptableSet)
//                 {
//                     var clearMethod = runtimeSetType.GetMethod("ClearSet");
//                     clearMethod?.Invoke(scriptableSet, null); // Call ClearSet dynamically
//                 }
//             }
//         }
//     }
// }

public abstract class RuntimeSet<T> : ScriptableObject
{
// #if UNITY_EDITOR
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
//     private static void ClearOnPlayModeExit()
//     {
//         Debug.LogWarning("Runtime set cleanup registered.");
//         
//         EditorApplication.playModeStateChanged += (PlayModeStateChange state) =>
//         {
//             Debug.LogWarning("Runtime set cleanup happening...");
//
//             if (state == PlayModeStateChange.ExitingPlayMode)
//             {
//                 var foundSets = Resources.LoadAll("Resources", typeof(RuntimeSet<>));
//                 foreach (var set in foundSets)
//                 {
//                     Debug.LogWarning($"set name: {set.name}");
//                 }
//             }
//         };
//     }
// #endif
    
    public List<T> Items = new List<T>();

    public T this[int index]
    {
        get { return Items[index]; }
        set { Items[index] = value; }
    }
    
    private int prevCount = 0;

    public delegate void AddRemoveCallback(T item);
    public delegate void SetCallback();

    public AddRemoveCallback onIncrement;
    
    public AddRemoveCallback onDecrement;

    public SetCallback onEmptied;
    
    public SetCallback onUnemptied;
    
    public SetCallback onChanged;
    

    public void Clear() => Items.Clear();

    public virtual void Add(T thing)
    {
        prevCount = Items.Count;

        if (Items.Contains(thing))
        {
            Debug.LogWarning("tried to add a duplicate " + typeof(T).ToString(), this);
            return;
        }

        Items.Add(thing);

        onIncrement?.Invoke(thing);

        UpdateCallbacks();
    }

    public virtual void Remove(T thing)
    {
        prevCount = Items.Count;

        if (!Items.Contains(thing))
        {
            Debug.LogWarning(
                string.Format(
                    "tried to remove {0}, but missing from runtime set {1}",
                    typeof(T).ToString(),
                    this.name)
                );
            return;
        }

        if (Items.Contains(thing))
            Items.Remove(thing);

        onDecrement?.Invoke(thing);

        UpdateCallbacks();
    }
    
    public int Count => Items.Count;

    public void RemoveAt(int index)
    {
        prevCount = Items.Count;
        
        if (index >= Items.Count)
        {
            Debug.LogWarning("tried to remove a item that is out of range", this);
            return;
        }

        var item = Items[index];
        
        Items.RemoveAt(index);
        onDecrement?.Invoke(item);
        
        UpdateCallbacks();
    }

    private void UpdateCallbacks()
    {
        if (onEmptied != null && prevCount != 0 && Items.Count == 0)
            onEmptied();
        
        else if (onUnemptied != null && prevCount == 0 && Items.Count != 0)
            onUnemptied();

        onChanged?.Invoke();
    }


    [UsedImplicitly]
    public void ClearSet()
    {
        // Debug.LogWarning($"Clearing set: {name}");
        Items.Clear();
    }
}