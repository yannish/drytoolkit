using System.IO;
using UnityEditor;
using UnityEngine;

public class DebugReaderSetupWindow : EditorWindow
{
    private const string FolderPrefKey = "DebugReader.RootFolder";
    private const string DefaultFolder = "Assets/DebugSettings";

    private string _folder;

    // -------------------------------------------------------------------------

    public static void Open()
    {
        var w = GetWindow<DebugReaderSetupWindow>(true, "Debug Reader Setup");
        w.minSize = new Vector2(440, 400);
        w.maxSize = new Vector2(440, 400);
        w.ShowUtility();
    }

    private void OnEnable()
    {
        var registryFolder = DebugReaderCodegen.GetRootFolder();
        _folder = registryFolder ?? EditorPrefs.GetString(FolderPrefKey, DefaultFolder);
    }

    // -------------------------------------------------------------------------

    private void OnGUI()
    {
        var bold  = new GUIStyle(EditorStyles.boldLabel);
        var small = new GUIStyle(EditorStyles.label) { fontSize = 10, wordWrap = true };
        var code  = new GUIStyle(EditorStyles.helpBox) { wordWrap = false };

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug Reader Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        DrawDivider();
        DrawStep1(bold, small);
        DrawDivider();
        DrawStep2(bold, small);
        DrawDivider();
        DrawStep3(bold, small, code);
    }

    // ── Step 1 ────────────────────────────────────────────────────────────────

    private void DrawStep1(GUIStyle bold, GUIStyle small)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("1  —  Choose a location", bold);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(
            "Choose where to create the registry. If your project uses Assembly " +
            "Definitions, place this inside a folder that is already part of your " +
            "runtime assembly — DebugReader.cs will be generated alongside it and " +
            "will automatically belong to the same assembly.",
            small);
        EditorGUILayout.Space(6);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Folder", GUILayout.Width(46));
            _folder = EditorGUILayout.TextField(_folder);

            if (GUILayout.Button("Browse", GUILayout.Width(58)))
            {
                var abs = EditorUtility.OpenFolderPanel("Choose folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    if (!abs.Replace('\\', '/').Contains(Application.dataPath.Replace('\\', '/')))
                        EditorUtility.DisplayDialog("Invalid folder", "Please choose a folder inside Assets.", "OK");
                    else
                    {
                        _folder = ("Assets" + abs.Substring(Application.dataPath.Length)).Replace('\\', '/');
                        EditorPrefs.SetString(FolderPrefKey, _folder);
                    }
                }
            }
        }

        EditorGUILayout.Space(2);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Use default  (Assets/DebugSettings)", GUILayout.Width(240)))
            {
                _folder = DefaultFolder;
                EditorPrefs.SetString(FolderPrefKey, _folder);
            }
        }
        EditorGUILayout.Space(6);
    }

    // ── Step 2 ────────────────────────────────────────────────────────────────

    private void DrawStep2(GUIStyle bold, GUIStyle small)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("2  —  Create the registry", bold);
        EditorGUILayout.Space(2);

        bool registryExists = AssetDatabase.FindAssets("t:DebugReaderRegistry").Length > 0;

        if (registryExists)
        {
            var path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:DebugReaderRegistry")[0]);
            EditorGUILayout.LabelField($"✓  Registry already exists at  {path}", small);
        }
        else
        {
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_folder)))
            {
                if (GUILayout.Button("Create Registry"))
                    CreateRegistry();
            }
        }

        EditorGUILayout.Space(6);
    }

    private void CreateRegistry()
    {
        CreateFolderRecursive(_folder);

        var assetPath = $"{_folder}/DebugReaderRegistry.asset";
        var registry  = CreateInstance<DebugReaderRegistry>();
        AssetDatabase.CreateAsset(registry, assetPath);
        AssetDatabase.SaveAssets();
        EditorPrefs.SetString(FolderPrefKey, _folder);

        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);
    }

    // ── Step 3 ────────────────────────────────────────────────────────────────

    private void DrawStep3(GUIStyle bold, GUIStyle small, GUIStyle code)
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("3  —  Use it", bold);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(
            "Create settings via the registry inspector (+ Bool, + Float, + Color). " +
            "DebugReader.cs is generated automatically. Access settings in code " +
            "inside a #if UNITY_EDITOR block:",
            small);
        EditorGUILayout.Space(4);
        EditorGUILayout.SelectableLabel(
            "#if UNITY_EDITOR\nif (DebugReader.Climbing.ShowRaycasts) { ... }\n#endif",
            code, GUILayout.Height(48));
        EditorGUILayout.Space(6);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void DrawDivider()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 1f));
    }

    private static void CreateFolderRecursive(string path)
    {
        var parts   = path.Replace('\\', '/').Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
