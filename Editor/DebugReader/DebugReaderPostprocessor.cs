using System.IO;
using UnityEditor;

public class DebugReaderPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool affected =
            AnyAreDebugSettings(importedAssets) ||
            AnyAreDebugSettings(movedAssets) ||
            AnyAreDeletedDebugSettings(deletedAssets) ||
            AnyAreDeletedDebugSettings(movedFromAssetPaths);

        if (!affected) return;

        // Defer codegen until after the current import cycle is fully finished.
        // Calling AssetDatabase operations directly inside OnPostprocessAllAssets
        // re-enters the importer and causes an infinite loop.
        // Deduplication via -= then += ensures we only run once even if multiple
        // assets change in the same batch.
        EditorApplication.delayCall -= RunCodegen;
        EditorApplication.delayCall += RunCodegen;
    }

    private static void RunCodegen()
    {
        DebugReaderRuntime.InvalidateCache();

        var registryGuids = AssetDatabase.FindAssets("t:DebugReaderRegistry");
        if (registryGuids.Length == 0) return;

        var registryPath   = AssetDatabase.GUIDToAssetPath(registryGuids[0]);
        var registryFolder = Path.GetDirectoryName(registryPath).Replace('\\', '/');
        var registry       = AssetDatabase.LoadAssetAtPath<DebugReaderRegistry>(registryPath);

        DebugReaderCodegen.OrganizeAssets(registryFolder);
        DebugReaderCodegen.RefreshRegistry(registry);

        if (registry.autoRefresh)
            DebugReaderCodegen.GenerateCode();
    }

    private static bool AnyAreDebugSettings(string[] paths)
    {
        foreach (var path in paths)
        {
            if (!path.EndsWith(".asset")) continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type == typeof(DebugReaderBool) || type == typeof(DebugReaderFloat) || type == typeof(DebugReaderColor) ||
                type == typeof(DebugReaderVector2) || type == typeof(DebugReaderVector3))
                return true;
        }
        return false;
    }

    // For deleted/moved-from paths the asset is gone, so we can't check type.
    // We trigger conservatively on any .asset deletion and let codegen re-scan.
    private static bool AnyAreDeletedDebugSettings(string[] paths)
    {
        foreach (var path in paths)
            if (path.EndsWith(".asset")) return true;
        return false;
    }
}
