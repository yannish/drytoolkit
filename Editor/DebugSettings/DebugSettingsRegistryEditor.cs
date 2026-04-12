using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DebugSettingsRegistry))]
public class DebugSettingsRegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var registry = (DebugSettingsRegistry)target;

        EditorGUILayout.Space(4);

        var registryFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(registry)).Replace('\\', '/');

        using (new EditorGUILayout.HorizontalScope())
        {
            var refreshLabel = registry.autoRefresh ? "Refresh" : "Refresh & Regenerate";
            if (GUILayout.Button(refreshLabel))
            {
                DebugSettingsCodegen.OrganizeAssets(registryFolder);
                DebugSettingsCodegen.RefreshRegistry(registry);
                DebugSettingsCodegen.GenerateCode();
            }

            var prevColor = GUI.color;
            GUI.color = registry.autoRefresh ? prevColor : new Color(1f, 0.85f, 0.4f);
            var newAutoRefresh = GUILayout.Toggle(registry.autoRefresh, "Auto-regenerate", GUI.skin.button, GUILayout.Width(120));
            GUI.color = prevColor;

            if (newAutoRefresh != registry.autoRefresh)
            {
                registry.autoRefresh = newAutoRefresh;
                EditorUtility.SetDirty(registry);
            }
        }

        EditorGUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+ Bool"))  DebugSettingCreateWindow.Show(typeof(DebugBool),  registryFolder, registry);
            if (GUILayout.Button("+ Float")) DebugSettingCreateWindow.Show(typeof(DebugFloat), registryFolder, registry);
            if (GUILayout.Button("+ Color")) DebugSettingCreateWindow.Show(typeof(DebugColor), registryFolder, registry);
        }

        EditorGUILayout.Space(8);

        // Union groups from settings assets and [DebugCommand] methods
        var settingsByGroup = registry.Settings
            .Where(s => s != null)
            .GroupBy(s => s.GroupName)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SettingName).ToList());

        var commandsByGroup = DebugCommandCache.Commands
            .GroupBy(kvp => GroupOf(kvp.Key))
            .ToDictionary(g => g.Key, g => g.OrderBy(kvp => kvp.Key).ToList());

        var allGroups = settingsByGroup.Keys
            .Union(commandsByGroup.Keys)
            .OrderBy(g => g);

        if (!allGroups.Any())
        {
            EditorGUILayout.HelpBox(
                "No debug settings found. Create Debug Settings assets (right-click > Create > Debug Settings) then hit Refresh.",
                MessageType.Info);
            return;
        }

        foreach (var groupName in allGroups)
        {
            settingsByGroup.TryGetValue(groupName, out var groupSettings);
            commandsByGroup.TryGetValue(groupName, out var groupCommands);
            DrawGroup(registry, groupName, groupSettings, groupCommands);
        }
    }

    private void DrawGroup(
        DebugSettingsRegistry registry,
        string groupName,
        List<DebugSettingBase> settings,
        List<KeyValuePair<string, MethodInfo>> commands)
    {
        EditorGUILayout.Space(4);

        bool isMuted = registry.IsGroupMuted(groupName);
        bool foldout = registry.GetGroupFoldout(groupName);

        using (new EditorGUILayout.HorizontalScope())
        {
            bool newFoldout = EditorGUILayout.Foldout(foldout, groupName, true, EditorStyles.foldoutHeader);
            if (newFoldout != foldout)
            {
                registry.SetGroupFoldout(groupName, newFoldout);
                EditorUtility.SetDirty(registry);
            }

            var prevColor = GUI.color;
            GUI.color = isMuted ? new Color(1f, 0.55f, 0.3f) : prevColor;
            if (GUILayout.Button(isMuted ? "Unmute" : "Mute", GUILayout.Width(64)))
            {
                registry.SetGroupMuted(groupName, !isMuted);
                EditorUtility.SetDirty(registry);
            }
            GUI.color = prevColor;
        }

        if (!foldout) return;

        EditorGUI.indentLevel++;

        if (settings != null)
            foreach (var setting in settings)
            {
                if (setting == null) continue;
                DrawSetting(setting, isMuted);
            }

        if (commands != null && commands.Count > 0)
        {
            if (settings != null && settings.Count > 0)
                EditorGUILayout.Space(2);

            DrawCommands(groupName, commands);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawCommands(string groupName, List<KeyValuePair<string, MethodInfo>> commands)
    {
        bool inPlayMode = EditorApplication.isPlaying;

        using (new EditorGUI.DisabledScope(!inPlayMode))
        {
            foreach (var (key, method) in commands)
            {
                var label = key.Substring(groupName.Length + 1); // strip "Group." prefix
                if (GUILayout.Button(label))
                {
                    try   { method.Invoke(null, null); }
                    catch (System.Exception e) { Debug.LogException(e); }
                }
            }
        }

        if (!inPlayMode)
            EditorGUILayout.HelpBox("Commands are only available in Play Mode.", MessageType.None);
    }

    private void DrawSetting(DebugSettingBase setting, bool groupMuted)
    {
        using var so = new SerializedObject(setting);
        so.Update();

        var valueProp = so.FindProperty("value");
        if (valueProp == null) return;

        using (new EditorGUI.DisabledScope(groupMuted))
            EditorGUILayout.PropertyField(valueProp, new GUIContent(setting.SettingName));

        so.ApplyModifiedProperties();
    }

    private static string GroupOf(string key)
    {
        int dot = key.IndexOf('.');
        return dot > 0 ? key.Substring(0, dot) : key;
    }
}
