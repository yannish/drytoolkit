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

    // ── Generated-code hot path ───────────────────────────────────────────────

    // Called once per property per domain load to resolve the backing SO reference.
    // After the first call the generated code holds the reference directly and never
    // calls this again, so the linear scan in GetSetting is not a hot-path concern.
    public static T Resolve<T>(string key) where T : DebugReaderSettingBase
    {
        var asset = Registry?.GetSetting(key) as T;
        if (asset == null)
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
        return asset;
    }

    public static bool IsGroupMuted(string groupName) => Registry?.IsGroupMuted(groupName) ?? false;

    // ── Direct callers (not generated code) ──────────────────────────────────

    public static bool GetBool(string key)
    {
        var reg = Registry;
        if (reg == null) return false;
        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return false;
        }
        if (reg.IsGroupMuted(asset.GroupName)) return false;
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

    public static Vector2 GetVector2(string key)
    {
        var reg = Registry;
        if (reg == null) return Vector2.zero;
        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return Vector2.zero;
        }
        return ((DebugReaderVector2)asset).value;
    }

    public static Vector3 GetVector3(string key)
    {
        var reg = Registry;
        if (reg == null) return Vector3.zero;
        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return Vector3.zero;
        }
        return ((DebugReaderVector3)asset).value;
    }

    public static void SetBool(string key, bool value)
    {
        var reg = Registry;
        if (reg == null) return;
        var asset = reg.GetSetting(key);
        if (asset == null)
        {
            Debug.LogWarning($"[DebugReader] No asset found for '{key}'. It may have been renamed or deleted — update or remove the DebugReader callsite.");
            return;
        }
        ((DebugReaderBool)asset).value = value;
        EditorUtility.SetDirty(asset);
    }

    public static void InvalidateCache() => _registry = null;
}
#endif
