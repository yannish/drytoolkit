using UnityEditor;
using UnityEngine;

public static class DebugReaderMenuItems
{
    [MenuItem("Tools/Debug Reader/Setup")]
    private static void OpenSetup() => DebugReaderSetupWindow.Open();

    // Ctrl+Shift+D — open the Debug Reader window and focus the search field
    [MenuItem("Tools/Debug Reader/Open Window %#d")]
    private static void OpenWindow() => DebugReaderWindow.Open();

    [MenuItem("Tools/Debug Reader/Select Registry Asset")]
    private static void SelectRegistry()
    {
        var guids = AssetDatabase.FindAssets("t:DebugReaderRegistry");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[DebugReader] No registry found. Run Tools > Debug Reader > Setup first.");
            return;
        }

        var asset = AssetDatabase.LoadAssetAtPath<DebugReaderRegistry>(
            AssetDatabase.GUIDToAssetPath(guids[0]));

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    // Exposed so the gitignore pref can be reset if the entry path changes
    internal static void ResetGitignorePref() => EditorPrefs.DeleteKey("DebugReader.GitignorePrompted");
}
