// Accessor layer used by generated DebugReader code.
// Wrapped in UNITY_EDITOR since it relies on AssetDatabase — DebugReader callsites
// must also be inside #if UNITY_EDITOR blocks, which is the expected usage.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class DebugReaderRuntime
{
    private static DebugReaderRegistry _registry;

    private static DebugReaderRegistry Registry
    {
        get
        {
            if (_registry != null) return _registry;
            var guids = AssetDatabase.FindAssets("t:DebugReaderRegistry");
            if (guids.Length > 0)
                _registry = AssetDatabase.LoadAssetAtPath<DebugReaderRegistry>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
            return _registry;
        }
    }

    public static bool GetBool(string key)
    {
        var reg = Registry;
        if (reg == null) return false;

        if (reg.IsGroupMuted(GroupOf(key))) return false;

        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return false;
        }
        return ((DebugReaderBool)asset).value;
    }

    public static float GetFloat(string key)
    {
        var reg = Registry;
        if (reg == null) return 0f;

        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return 0f;
        }
        return ((DebugReaderFloat)asset).value;
    }

    public static Color GetColor(string key)
    {
        var reg = Registry;
        if (reg == null) return Color.white;

        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return Color.white;
        }
        return ((DebugReaderColor)asset).value;
    }

    public static void InvalidateCache() => _registry = null;

    private static string GroupOf(string key)
    {
        int dot = key.IndexOf('.');
        return dot > 0 ? key.Substring(0, dot) : string.Empty;
    }
}
#endif
