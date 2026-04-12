using UnityEditor;
using UnityEngine;

public static class DebugSettingsMenuItems
{
    [MenuItem("Tools/Debug Settings/Setup")]
    private static void OpenSetup() => DebugSettingsSetupWindow.Open();

    // Ctrl+Shift+D — select and ping the registry in the Project window
    [MenuItem("Tools/Debug Settings/Select Registry %#d")]
    private static void SelectRegistry()
    {
        var guids = AssetDatabase.FindAssets("t:DebugSettingsRegistry");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[DebugSettings] No registry found. Run Tools > Debug Settings > Setup first.");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<DebugSettingsRegistry>(
            AssetDatabase.GUIDToAssetPath(guids[0]));

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // Exposed so the gitignore pref can be reset if the entry path changes
    internal static void ResetGitignorePref() => EditorPrefs.DeleteKey("DebugSettings.GitignorePrompted");
}
