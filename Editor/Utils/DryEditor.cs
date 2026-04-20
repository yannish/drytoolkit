using System;
using System.Collections.Generic;
using System.Reflection;
using drytoolkit.Runtime;
using drytoolkit.Runtime.Utils;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using Object = UnityEngine.Object;

namespace drytoolkit.Editor.Utils
{
    [CustomEditor(typeof(DryMonoBehaviour), true)]
    internal class DryMonoBehaviourEditor : DryEditor { }

    [CustomEditor(typeof(DryScriptableObject), true)]
    internal class DryScriptableObjectEditor : DryEditor { }

    public abstract class DryEditor : UnityEditor.Editor
    {
        // ── Segment types ──────────────────────────────────────────────────────

        private abstract class Segment { }

        private class UngroupedSegment : Segment
        {
            public readonly List<string> propertyPaths = new List<string>();
        }

        private class GroupedSegment : Segment
        {
            public string title;
            public string titleFrom;
            public int groupIndex;
            public readonly List<string> propertyPaths = new List<string>();
        }

        // ── Button types ───────────────────────────────────────────────────────

        private class ButtonInfo
        {
            public MethodInfo method;
            public string label;
        }

        private class ButtonRow
        {
            public readonly List<ButtonInfo> buttons = new List<ButtonInfo>();
        }

        // ── Static caches (cleared on domain reload) ───────────────────────────

        private static readonly Dictionary<Type, List<Segment>> _segmentCache
            = new Dictionary<Type, List<Segment>>();

        private static readonly Dictionary<Type, (List<ButtonRow> top, List<ButtonRow> bottom)> _buttonCache
            = new Dictionary<Type, (List<ButtonRow>, List<ButtonRow>)>();

        private static readonly Dictionary<(Type, string), Func<object, string>> _titleFromCache
            = new Dictionary<(Type, string), Func<object, string>>();

        private static readonly Dictionary<(int, int), bool> _expandedState
            = new Dictionary<(int, int), bool>();

        // ── Per-editor-instance state ──────────────────────────────────────────

        private List<Segment> _segments;
        private List<AnimBool> _animBools;
        private List<ButtonRow> _topButtons;
        private List<ButtonRow> _bottomButtons;

        // ── Unity callbacks ────────────────────────────────────────────────────

        protected virtual void OnEnable()
        {
            _segments = BuildOrFetchSegments(target.GetType());
            var buttonRows = BuildOrFetchButtonRows(target.GetType());
            _topButtons = buttonRows.top;
            _bottomButtons = buttonRows.bottom;

            _animBools = new List<AnimBool>();
            foreach (var seg in _segments)
            {
                if (seg is GroupedSegment g)
                {
                    bool expanded = GetExpandedState(target.GetInstanceID(), g.groupIndex);
                    var ab = new AnimBool(expanded);
                    ab.valueChanged.AddListener(Repaint);
                    _animBools.Add(ab);
                }
            }
        }

        protected virtual void OnDisable()
        {
            if (_animBools == null) return;
            foreach (var ab in _animBools)
                ab.valueChanged.RemoveListener(Repaint);
            _animBools = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            serializedObject.DrawScriptField();

            DrawButtonRows(_topButtons);

            foreach (var seg in _segments)
            {
                if (seg is UngroupedSegment u)
                    DrawUngrouped(u);
                else if (seg is GroupedSegment g)
                    DrawGroup(g);
            }

            DrawButtonRows(_bottomButtons);

            serializedObject.ApplyModifiedProperties();
        }

        // ── Styles ────────────────────────────────────────────────────────────

        private static GUIStyle _groupFoldoutStyle;
        private static GUIStyle GroupFoldoutStyle => _groupFoldoutStyle ??= new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold
        };

        // ── Rendering ─────────────────────────────────────────────────────────

        private void DrawUngrouped(UngroupedSegment seg)
        {
            foreach (var path in seg.propertyPaths)
            {
                var prop = serializedObject.FindProperty(path);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }
        }

        private void DrawGroup(GroupedSegment seg)
        {
            var animBool = _animBools[seg.groupIndex];
            int instanceID = target.GetInstanceID();

            bool isExpanded = GetExpandedState(instanceID, seg.groupIndex);
            string title = ResolveTitle(seg);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.indentLevel++;
                isExpanded = EditorGUILayout.Foldout(isExpanded, title, true, GroupFoldoutStyle);
                SetExpandedState(instanceID, seg.groupIndex, isExpanded);
                animBool.target = isExpanded;

                if (EditorGUILayout.BeginFadeGroup(animBool.faded))
                {
                    foreach (var path in seg.propertyPaths)
                    {
                        var prop = serializedObject.FindProperty(path);
                        if (prop != null)
                            EditorGUILayout.PropertyField(prop, true);
                    }
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
        }

        // ── Button rendering ───────────────────────────────────────────────────

        private void DrawButtonRows(List<ButtonRow> rows)
        {
            foreach (var row in rows)
            {
                if (row.buttons.Count == 1)
                {
                    var btn = row.buttons[0];
                    if (GUILayout.Button(btn.label))
                        btn.method.Invoke(target, null);
                }
                else
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        foreach (var btn in row.buttons)
                        {
                            if (GUILayout.Button(btn.label))
                                btn.method.Invoke(target, null);
                        }
                    }
                }
            }
        }

        // ── Button building ────────────────────────────────────────────────────

        private static (List<ButtonRow> top, List<ButtonRow> bottom) BuildOrFetchButtonRows(Type type)
        {
            if (_buttonCache.TryGetValue(type, out var cached))
                return cached;

            var topRows = new List<ButtonRow>();
            var bottomRows = new List<ButtonRow>();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
                                     | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var typeChain = new Stack<Type>();
            for (Type t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject)
                                          && t != typeof(Behaviour) && t != typeof(Component)
                                          && t != typeof(Object) && t != typeof(object); t = t.BaseType)
            {
                typeChain.Push(t);
            }

            while (typeChain.Count > 0)
            {
                foreach (var method in typeChain.Pop().GetMethods(flags))
                {
                    var attr = method.GetCustomAttribute<EditorButtonAttribute>();
                    if (attr == null || method.GetParameters().Length > 0) continue;

                    string label = attr.label ?? ObjectNames.NicifyVariableName(method.Name);
                    var info = new ButtonInfo { method = method, label = label };
                    var targetList = attr.position == ButtonPosition.Top ? topRows : bottomRows;

                    if (attr.group != null)
                    {
                        var last = targetList.Count > 0 ? targetList[targetList.Count - 1] : null;
                        if (last != null && last.buttons.Count > 0
                            && last.buttons[0].method.GetCustomAttribute<EditorButtonAttribute>()?.group == attr.group)
                        {
                            last.buttons.Add(info);
                        }
                        else
                        {
                            var row = new ButtonRow();
                            row.buttons.Add(info);
                            targetList.Add(row);
                        }
                    }
                    else
                    {
                        var row = new ButtonRow();
                        row.buttons.Add(info);
                        targetList.Add(row);
                    }
                }
            }

            var result = (topRows, bottomRows);
            _buttonCache[type] = result;
            return result;
        }

        // ── Segment building ───────────────────────────────────────────────────

        private static List<Segment> BuildOrFetchSegments(Type type)
        {
            if (_segmentCache.TryGetValue(type, out var cached))
                return cached;

            var fields = GetSerializedFieldsInOrder(type);
            var segments = new List<Segment>();
            GroupedSegment currentGroup = null;
            int groupIndex = 0;

            foreach (var field in fields)
            {
                var foldoutAttr = field.GetCustomAttribute<FoldAttribute>();
                var endAttr = field.GetCustomAttribute<EndFoldAttribute>();

                if (foldoutAttr != null)
                {
                    if (currentGroup != null)
                        segments.Add(currentGroup);

                    currentGroup = new GroupedSegment
                    {
                        title = foldoutAttr.title,
                        titleFrom = foldoutAttr.titleFrom,
                        groupIndex = groupIndex++
                    };
                    currentGroup.propertyPaths.Add(field.Name);

                    if (endAttr != null)
                    {
                        segments.Add(currentGroup);
                        currentGroup = null;
                    }
                }
                else if (currentGroup != null)
                {
                    currentGroup.propertyPaths.Add(field.Name);

                    if (endAttr != null)
                    {
                        segments.Add(currentGroup);
                        currentGroup = null;
                    }
                }
                else
                {
                    if (segments.Count == 0 || !(segments[segments.Count - 1] is UngroupedSegment))
                        segments.Add(new UngroupedSegment());

                    ((UngroupedSegment)segments[segments.Count - 1]).propertyPaths.Add(field.Name);
                }
            }

            if (currentGroup != null)
                segments.Add(currentGroup);

            _segmentCache[type] = segments;
            return segments;
        }

        private static List<FieldInfo> GetSerializedFieldsInOrder(Type type)
        {
            // Walk up to (but not including) MonoBehaviour / ScriptableObject, preserving order base→derived.
            var typeChain = new Stack<Type>();
            for (Type t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject)
                                          && t != typeof(Behaviour) && t != typeof(Component)
                                          && t != typeof(UnityEngine.Object) && t != typeof(object); t = t.BaseType)
            {
                typeChain.Push(t);
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
                                     | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var result = new List<FieldInfo>();
            while (typeChain.Count > 0)
            {
                foreach (var field in typeChain.Pop().GetFields(flags))
                {
                    if (field.IsStatic || field.IsLiteral) continue;
                    bool isPublic = field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null;
                    bool isSerializeField = !field.IsPublic && field.GetCustomAttribute<SerializeField>() != null;
                    if (isPublic || isSerializeField)
                        result.Add(field);
                }
            }
            return result;
        }

        // ── Title resolution ───────────────────────────────────────────────────

        private string ResolveTitle(GroupedSegment seg)
        {
            if (seg.titleFrom == null)
                return seg.title;

            var key = (target.GetType(), seg.titleFrom);
            if (!_titleFromCache.TryGetValue(key, out var del))
            {
                del = BuildStringDelegate(target.GetType(), seg.titleFrom);
                _titleFromCache[key] = del;
            }

            if (del == null)
                return seg.title;

            string resolved = del(target);
            return string.IsNullOrEmpty(resolved) ? seg.title : resolved;
        }

        private static Func<object, string> BuildStringDelegate(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public
                                     | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo fi = t.GetField(memberName, flags);
                if (fi != null && fi.FieldType == typeof(string))
                    return obj => (string)fi.GetValue(obj);

                PropertyInfo pi = t.GetProperty(memberName, flags);
                if (pi != null && pi.CanRead && pi.PropertyType == typeof(string))
                    return obj => (string)pi.GetValue(obj);
            }

            return null;
        }

        // ── Expanded state helpers ─────────────────────────────────────────────

        private static bool GetExpandedState(int instanceID, int groupIndex)
        {
            _expandedState.TryGetValue((instanceID, groupIndex), out bool expanded);
            return expanded;
        }

        private static void SetExpandedState(int instanceID, int groupIndex, bool expanded)
        {
            _expandedState[(instanceID, groupIndex)] = expanded;
        }
    }
}
