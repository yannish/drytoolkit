using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ClipBindingRenamerWindow : EditorWindow
{
    private AnimationClip              _clip;
    private GameObject                 _rootGO;
    private List<string>               _uniquePaths = new();
    private Dictionary<string, string> _newPaths    = new();
    private Vector2                    _scroll;

    // Cached hierarchy paths derived from _rootGO
    private List<string> _hierarchyPaths = new();
    private GameObject   _cachedRootGO;

    [MenuItem("Window/Animation/Clip Binding Renamer")]
    public static void Open()
    {
        var w = GetWindow<ClipBindingRenamerWindow>("Clip Binding Renamer");
        w.minSize = new Vector2(480, 300);
        w.Show();
    }

    // ── OnGUI ─────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.Space(6);

        using (var cc = new EditorGUI.ChangeCheckScope())
        {
            _clip = (AnimationClip)EditorGUILayout.ObjectField("Clip", _clip, typeof(AnimationClip), false);
            if (cc.changed) RefreshPaths();
        }

        using (var cc = new EditorGUI.ChangeCheckScope())
        {
            _rootGO = (GameObject)EditorGUILayout.ObjectField("Root (optional)", _rootGO, typeof(GameObject), true);
            if (cc.changed) RefreshHierarchyPaths();
        }

        if (_clip == null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox("Assign an AnimationClip to begin.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6);
        DrawDivider();

        DrawColumnHeaders();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var path in _uniquePaths)
            DrawRow(path);
        EditorGUILayout.EndScrollView();

        DrawDivider();

        var collisions = FindCollisions();
        foreach (var c in collisions)
            EditorGUILayout.HelpBox($"\"{c}\" already exists in the clip and is not being vacated. Rebind or clear that path first.", MessageType.Warning);

        EditorGUILayout.Space(4);

        var pendingRenames = _newPaths.Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != kvp.Key).ToList();
        bool hasWork      = pendingRenames.Count > 0;

        using (new EditorGUI.DisabledScope(!hasWork || collisions.Count > 0))
        {
            if (GUILayout.Button($"Apply Renames ({pendingRenames.Count})"))
                ApplyRenames(pendingRenames);
        }

        EditorGUILayout.Space(6);
    }

    // ── Rows ──────────────────────────────────────────────────────────────────

    private void DrawColumnHeaders()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Current path", EditorStyles.miniLabel, GUILayout.MinWidth(140));
            EditorGUILayout.LabelField("New path", EditorStyles.miniLabel, GUILayout.MinWidth(140));
            if (_rootGO != null)
                GUILayout.Space(26);
            EditorGUILayout.LabelField("Props", EditorStyles.miniLabel, GUILayout.Width(36));
        }
    }

    private void DrawRow(string path)
    {
        _newPaths.TryGetValue(path, out var current);
        int propCount = CountProperties(path);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(path) ? "<root>" : path, GUILayout.MinWidth(140));

            var typed = EditorGUILayout.TextField(current ?? "", GUILayout.MinWidth(140));
            if (typed != current)
                _newPaths[path] = typed;

            // Hierarchy picker popup
            if (_rootGO != null)
            {
                if (GUILayout.Button("▾", GUILayout.Width(22)))
                    ShowHierarchyMenu(path, typed ?? "");
            }

            EditorGUILayout.LabelField(propCount.ToString(), GUILayout.Width(36));
        }
    }

    // ── Hierarchy menu ────────────────────────────────────────────────────────

    private void ShowHierarchyMenu(string oldPath, string filter)
    {
        var menu    = new GenericMenu();
        var matches = string.IsNullOrEmpty(filter)
            ? _hierarchyPaths
            : _hierarchyPaths.Where(p => p.ToLowerInvariant().Contains(filter.ToLowerInvariant())).ToList();

        if (matches.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No matches"));
        }
        else
        {
            foreach (var p in matches)
            {
                var captured = p;
                menu.AddItem(new GUIContent(string.IsNullOrEmpty(captured) ? "<root>" : captured), false, () =>
                {
                    _newPaths[oldPath] = captured;
                    Repaint();
                });
            }
        }

        menu.ShowAsContext();
    }

    // ── Collision detection ───────────────────────────────────────────────────

    private List<string> FindCollisions()
    {
        var renames   = PendingRenames();
        var vacating  = new HashSet<string>(renames.Keys);
        var collisions = new List<string>();

        foreach (var (_, newPath) in renames)
        {
            if (string.IsNullOrEmpty(newPath)) continue;
            if (_uniquePaths.Contains(newPath) && !vacating.Contains(newPath))
                collisions.Add(newPath);
        }

        return collisions.Distinct().ToList();
    }

    private Dictionary<string, string> PendingRenames() =>
        _newPaths
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value) && kvp.Value != kvp.Key)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    // ── Apply ─────────────────────────────────────────────────────────────────

    private void ApplyRenames(List<KeyValuePair<string, string>> renames)
    {
        Undo.RecordObject(_clip, "Rename Clip Bindings");

        var curveBindings  = AnimationUtility.GetCurveBindings(_clip);
        var objRefBindings = AnimationUtility.GetObjectReferenceCurveBindings(_clip);

        var renameMap = renames.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        // Float curves
        foreach (var binding in curveBindings)
        {
            if (!renameMap.TryGetValue(binding.path, out var newPath)) continue;
            var curve   = AnimationUtility.GetEditorCurve(_clip, binding);
            var newBind = binding;
            newBind.path = newPath;
            AnimationUtility.SetEditorCurve(_clip, binding, null);
            AnimationUtility.SetEditorCurve(_clip, newBind, curve);
        }

        // Object-reference curves
        foreach (var binding in objRefBindings)
        {
            if (!renameMap.TryGetValue(binding.path, out var newPath)) continue;
            var keys    = AnimationUtility.GetObjectReferenceCurve(_clip, binding);
            var newBind = binding;
            newBind.path = newPath;
            AnimationUtility.SetObjectReferenceCurve(_clip, binding, null);
            AnimationUtility.SetObjectReferenceCurve(_clip, newBind, keys);
        }

        EditorUtility.SetDirty(_clip);
        AssetDatabase.SaveAssets();

        // Clear applied renames and refresh
        foreach (var kvp in renames)
            _newPaths.Remove(kvp.Key);

        RefreshPaths();
        Repaint();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RefreshPaths()
    {
        _uniquePaths.Clear();
        _newPaths.Clear();

        if (_clip == null) return;

        var paths = new HashSet<string>();
        foreach (var b in AnimationUtility.GetCurveBindings(_clip))
            paths.Add(b.path);
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(_clip))
            paths.Add(b.path);

        _uniquePaths = paths.OrderBy(p => p).ToList();
    }

    private void RefreshHierarchyPaths()
    {
        _hierarchyPaths.Clear();
        _cachedRootGO = _rootGO;

        if (_rootGO == null) return;

        var root = _rootGO.transform;
        foreach (var t in _rootGO.GetComponentsInChildren<Transform>(true))
            _hierarchyPaths.Add(AnimationUtility.CalculateTransformPath(t, root));

        _hierarchyPaths.Sort();
    }

    private int CountProperties(string path)
    {
        int count = 0;
        foreach (var b in AnimationUtility.GetCurveBindings(_clip))
            if (b.path == path) count++;
        foreach (var b in AnimationUtility.GetObjectReferenceCurveBindings(_clip))
            if (b.path == path) count++;
        return count;
    }

    private static void DrawDivider()
    {
        EditorGUILayout.Space(4);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 0.35f));
        EditorGUILayout.Space(4);
    }
}
