using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class DebugReaderWindow : EditorWindow
{
    private DebugReaderRegistry _registry;
    private Vector2             _scrollPos;
    private string              _searchQuery    = "";
    private bool                _wantFocus;
    private string              _registryFolder = "";

    // Pin icons — loaded once per session from Assets/drytoolkit/Editor/Icons/.
    // Drop 16×16 PNGs named pin-on.png / pin-off.png there to replace the text fallback.
    private static GUIContent _iconPinOn;
    private static GUIContent _iconPinOff;

    private static GUIContent PinIcon(bool pinned)
    {
        ref var slot = ref pinned ? ref _iconPinOn : ref _iconPinOff;
        if (slot != null) return slot;

        var path = pinned
            ? "Assets/drytoolkit/Editor/Icons/pin-on.png"
            : "Assets/drytoolkit/Editor/Icons/pin-off.png";

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        slot = tex != null
            ? new GUIContent(tex,    pinned ? "Unpin"      : "Pin to top")
            : new GUIContent(pinned ? "●"  : "○", pinned ? "Unpin" : "Pin to top");
        return slot;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public static void Open()
    {
        var w = GetWindow<DebugReaderWindow>("Debug Reader");
        w.minSize = new Vector2(300, 200);
        w.Show();
        w.Focus();
    }

    private void OnEnable()
    {
        LoadRegistry();
        _wantFocus = true;
    }

    private void OnFocus()
    {
        _wantFocus = true;
    }

    private void LoadRegistry()
    {
        var guids = AssetDatabase.FindAssets("t:DebugReaderRegistry");
        _registry = guids.Length > 0
            ? AssetDatabase.LoadAssetAtPath<DebugReaderRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]))
            : null;
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (_registry == null)
        {
            LoadRegistry();
            if (_registry == null) { DrawNoRegistry(); return; }
        }

        _registryFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(_registry)).Replace('\\', '/');

        DrawToolbar();

        EditorGUILayout.Space(4);

        bool hasQuery = !string.IsNullOrEmpty(_searchQuery);
        var  query    = hasQuery ? _searchQuery.ToLowerInvariant() : null;

        var settingsByGroup = _registry.Settings
            .Where(s => s != null && (!hasQuery || s.FullKey.ToLowerInvariant().Contains(query)))
            .GroupBy(s => s.GroupName)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SettingName).ToList());

        var commandsByGroup = DebugReaderCommandCache.Commands
            .Where(kvp => !hasQuery || kvp.Key.ToLowerInvariant().Contains(query))
            .GroupBy(kvp => GroupOf(kvp.Key))
            .ToDictionary(g => g.Key, g => g.OrderBy(kvp => kvp.Key).ToList());

        var allGroups = _registry.Settings
            .Where(s => s != null)
            .Select(s => s.GroupName)
            .Union(DebugReaderCommandCache.Commands.Keys.Select(GroupOf))
            .Distinct()
            .OrderBy(g => g)
            .ToList();

        var visible  = allGroups.Where(g => settingsByGroup.ContainsKey(g) || commandsByGroup.ContainsKey(g)).ToList();
        var pinned   = visible.Where(g =>  _registry.IsGroupPinned(g)).ToList();
        var unpinned = visible.Where(g => !_registry.IsGroupPinned(g)).ToList();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(2);
                GUI.SetNextControlName("DebugReaderSearch");
                _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
                if (_wantFocus)
                {
                    EditorGUI.FocusTextInControl("DebugReaderSearch");
                    _wantFocus = false;
                }

                if (GUILayout.Button("Collapse All", GUILayout.Width(85)))
                    CollapseAll();
                if (GUILayout.Button("Expand All", GUILayout.Width(75)))
                    ExpandAll();
            }

            EditorGUILayout.Space(4);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (visible.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    hasQuery
                        ? $"No settings or commands match \"{_searchQuery}\"."
                        : "No debug settings found. Create Debug Reader assets (right-click > Create > Debug Reader) then hit Refresh.",
                    MessageType.Info);
            }
            else
            {
                foreach (var g in pinned)
                {
                    settingsByGroup.TryGetValue(g, out var gs);
                    commandsByGroup.TryGetValue(g, out var gc);
                    DrawGroup(g, gs, gc, forceExpand: hasQuery);
                }

                if (pinned.Count > 0 && unpinned.Count > 0)
                    DrawPinnedDivider();

                foreach (var g in unpinned)
                {
                    settingsByGroup.TryGetValue(g, out var gs);
                    commandsByGroup.TryGetValue(g, out var gc);
                    DrawGroup(g, gs, gc, forceExpand: hasQuery);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void DrawToolbar()
    {
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("CREATE:", EditorStyles.boldLabel);
                if (GUILayout.Button("+ Bool"))    DebugReaderCreateWindow.Show(typeof(DebugReaderBool),    _registryFolder, _registry);
                if (GUILayout.Button("+ Float"))   DebugReaderCreateWindow.Show(typeof(DebugReaderFloat),   _registryFolder, _registry);
                if (GUILayout.Button("+ Color"))   DebugReaderCreateWindow.Show(typeof(DebugReaderColor),   _registryFolder, _registry);
                if (GUILayout.Button("+ Vector2")) DebugReaderCreateWindow.Show(typeof(DebugReaderVector2), _registryFolder, _registry);
                if (GUILayout.Button("+ Vector3")) DebugReaderCreateWindow.Show(typeof(DebugReaderVector3), _registryFolder, _registry);
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh & Regenerate"))
                {
                    DebugReaderCodegen.OrganizeAssets(_registryFolder);
                    DebugReaderCodegen.RefreshRegistry(_registry);
                    DebugReaderCodegen.GenerateCode();
                    GUIUtility.ExitGUI();
                }

                var prevColor = GUI.color;
                GUI.color = _registry.autoRefresh ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.85f, 0.4f);
                var toggleLabel = _registry.autoRefresh ? "Auto-regen: ON" : "Auto-regen: OFF";
                var newAutoRefresh = GUILayout.Toggle(_registry.autoRefresh, toggleLabel, GUI.skin.button, GUILayout.Width(120));
                GUI.color = prevColor;

                if (newAutoRefresh != _registry.autoRefresh)
                {
                    _registry.autoRefresh = newAutoRefresh;
                    EditorUtility.SetDirty(_registry);
                }
            }
        }
    }

    // ── Pinned divider ────────────────────────────────────────────────────────

    private static void DrawPinnedDivider()
    {
        var lineColor = new Color(0.5f, 0.5f, 0.5f, 0.35f);
        EditorGUILayout.Space(6);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), lineColor);
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("PINNED", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.Space(2);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), lineColor);
        EditorGUILayout.Space(6);
    }

    // ── Group ─────────────────────────────────────────────────────────────────

    private void DrawGroup(
        string groupName,
        List<DebugReaderSettingBase> settings,
        List<KeyValuePair<string, MethodInfo>> commands,
        bool forceExpand = false)
    {
        EditorGUILayout.Space(4);

        bool isMuted = _registry.IsGroupMuted(groupName);
        bool foldout = forceExpand || _registry.GetGroupFoldout(groupName);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                var displayName = groupName.ToUpper();
                if (forceExpand)
                {
                    EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
                }
                else
                {
                    bool newFoldout = EditorGUILayout.Foldout(foldout, displayName, true, EditorStyles.boldLabel);
                    if (newFoldout != foldout)
                    {
                        _registry.SetGroupFoldout(groupName, newFoldout);
                        EditorUtility.SetDirty(_registry);
                    }
                }

                var prefill = groupName + ".";
                if (GUILayout.Button("+B",  GUILayout.Width(24))) DebugReaderCreateWindow.Show(typeof(DebugReaderBool),    _registryFolder, _registry, prefill);
                if (GUILayout.Button("+F",  GUILayout.Width(24))) DebugReaderCreateWindow.Show(typeof(DebugReaderFloat),   _registryFolder, _registry, prefill);
                if (GUILayout.Button("+C",  GUILayout.Width(24))) DebugReaderCreateWindow.Show(typeof(DebugReaderColor),   _registryFolder, _registry, prefill);
                if (GUILayout.Button("+V2", GUILayout.Width(30))) DebugReaderCreateWindow.Show(typeof(DebugReaderVector2), _registryFolder, _registry, prefill);
                if (GUILayout.Button("+V3", GUILayout.Width(30))) DebugReaderCreateWindow.Show(typeof(DebugReaderVector3), _registryFolder, _registry, prefill);

                GUILayout.Space(6);

                bool isPinned  = _registry.IsGroupPinned(groupName);
                var  prevColor = GUI.color;
                GUI.color = isPinned ? new Color(1f, 0.85f, 0.2f) : prevColor;
                if (GUILayout.Button(PinIcon(isPinned), GUILayout.Width(22), GUILayout.Height(18)))
                {
                    _registry.SetGroupPinned(groupName, !isPinned);
                    EditorUtility.SetDirty(_registry);
                    Repaint();
                }

                GUI.color = isMuted ? new Color(1f, 0.55f, 0.3f) : prevColor;
                if (GUILayout.Button(isMuted ? "Unmute" : "Mute", GUILayout.Width(64)))
                {
                    _registry.SetGroupMuted(groupName, !isMuted);
                    EditorUtility.SetDirty(_registry);
                }
                GUI.color = prevColor;
            }

            if (foldout)
            {
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
        }
    }

    private void DrawCommands(string groupName, List<KeyValuePair<string, MethodInfo>> commands)
    {
        bool inPlayMode = EditorApplication.isPlaying;

        using (new EditorGUI.DisabledScope(!inPlayMode))
        {
            foreach (var (key, method) in commands)
            {
                var label = key.Substring(groupName.Length + 1);
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

    private void DrawSetting(DebugReaderSettingBase setting, bool groupMuted)
    {
        using var so = new SerializedObject(setting);
        so.Update();

        var valueProp = so.FindProperty("value");
        if (valueProp == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(groupMuted))
                EditorGUILayout.PropertyField(valueProp, new GUIContent(setting.SettingName));

            // Object field — click to select/ping the backing SO in the Project window for renaming
            EditorGUILayout.ObjectField(setting, typeof(DebugReaderSettingBase), false, GUILayout.Width(300));
        }

        so.ApplyModifiedProperties();
    }

    // ── Collapse all ──────────────────────────────────────────────────────────

    private void CollapseAll() => SetAllFoldouts(false);
    private void ExpandAll()   => SetAllFoldouts(true);

    private void SetAllFoldouts(bool open)
    {
        if (_registry == null) return;

        var allGroups = _registry.Settings
            .Where(s => s != null)
            .Select(s => s.GroupName)
            .Union(DebugReaderCommandCache.Commands.Keys.Select(GroupOf))
            .Distinct();

        foreach (var group in allGroups)
            _registry.SetGroupFoldout(group, open);

        EditorUtility.SetDirty(_registry);
        Repaint();
    }

    // ── No registry ───────────────────────────────────────────────────────────

    private void DrawNoRegistry()
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.HelpBox("No registry found. Run Setup to create one.", MessageType.Info);
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Open Setup"))
            DebugReaderSetupWindow.Open();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GroupOf(string key)
    {
        int dot = key.IndexOf('.');
        return dot > 0 ? key.Substring(0, dot) : key;
    }
}
