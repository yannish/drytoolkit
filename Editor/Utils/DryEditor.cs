using System;
using System.Collections;
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
            public readonly List<SegmentItem> items = new List<SegmentItem>();
        }

        private class GroupedSegment : Segment
        {
            public string title;
            public string titleFrom;
            public int groupIndex;
            public readonly List<SegmentItem> items = new List<SegmentItem>();
        }

        // ── Segment item types ─────────────────────────────────────────────────

        private abstract class SegmentItem { }

        private class FieldItem : SegmentItem
        {
            public string path;
        }

        private class ReflectedFieldItem : SegmentItem
        {
            public FieldInfo field;
            public string countFrom;
        }

        private class ReflectedPropertyItem : SegmentItem
        {
            public PropertyInfo property;
            public string countFrom;
        }

        private class ButtonRowItem : SegmentItem
        {
            public string group;
            public readonly ButtonRow row = new ButtonRow();
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

        private static readonly Dictionary<Type, (List<Segment> segments, List<ButtonRow> top, List<ButtonRow> bottom)> _typeCache
            = new Dictionary<Type, (List<Segment>, List<ButtonRow>, List<ButtonRow>)>();

        private static readonly Dictionary<(Type, string), Func<object, string>> _titleFromCache
            = new Dictionary<(Type, string), Func<object, string>>();

        private static readonly Dictionary<(Type, string), Func<object, bool>> _showIfCache
            = new Dictionary<(Type, string), Func<object, bool>>();

        private static readonly Dictionary<(int, int), bool> _expandedState
            = new Dictionary<(int, int), bool>();

        private static readonly Dictionary<(Type, string), Func<object, int>> _countFromCache
            = new Dictionary<(Type, string), Func<object, int>>();

        private static readonly Dictionary<(int, string), bool> _foldoutState
            = new Dictionary<(int, string), bool>();

        // ── Per-editor-instance state ──────────────────────────────────────────

        private List<Segment> _segments = new List<Segment>();
        private List<AnimBool> _animBools = new List<AnimBool>();
        private List<ButtonRow> _topButtons = new List<ButtonRow>();
        private List<ButtonRow> _bottomButtons = new List<ButtonRow>();
        private bool _hasInspectableFields;

        // ── Unity callbacks ────────────────────────────────────────────────────

        protected virtual void OnEnable()
        {
            var (segments, top, bottom) = BuildOrFetchAll(target.GetType());
            _segments = segments;
            _topButtons = top;
            _bottomButtons = bottom;
            _hasInspectableFields = HasAnyReflectedField(segments);

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

        public override bool RequiresConstantRepaint() => _hasInspectableFields;

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

        private static GUIStyle _countLabelStyle;
        private static GUIStyle CountLabelStyle => _countLabelStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleRight
        };

        // ── Rendering ─────────────────────────────────────────────────────────

        private void DrawUngrouped(UngroupedSegment seg)
        {
            foreach (var item in seg.items)
                DrawSegmentItem(item);
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
                    foreach (var item in seg.items)
                        DrawSegmentItem(item);
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSegmentItem(SegmentItem item)
        {
            if (item is FieldItem fi)
            {
                var prop = serializedObject.FindProperty(fi.path);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }
            else if (item is ReflectedFieldItem rfi)
            {
                DrawReflectedField(rfi.field, rfi.countFrom);
            }
            else if (item is ReflectedPropertyItem rpi)
            {
                DrawReflectedProperty(rpi.property, rpi.countFrom);
            }
            else if (item is ButtonRowItem bri)
            {
                DrawButtonRow(bri.row);
            }
        }

        // ── Button rendering ───────────────────────────────────────────────────

        private void DrawButtonRows(List<ButtonRow> rows)
        {
            foreach (var row in rows)
                DrawButtonRow(row);
        }

        private void DrawButtonRow(ButtonRow row)
        {
            // Collect buttons that pass their showIf condition (if any).
            var visible = new List<ButtonInfo>(row.buttons.Count);
            foreach (var btn in row.buttons)
            {
                var attr = btn.method.GetCustomAttribute<EditorButtonAttribute>();
                if (attr.showIf == null || EvaluateShowIf(attr.showIf, attr.showIfValue, target, serializedObject))
                    visible.Add(btn);
            }

            if (visible.Count == 0) return;

            if (visible.Count == 1)
            {
                if (GUILayout.Button(visible[0].label))
                    visible[0].method.Invoke(target, null);
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    foreach (var btn in visible)
                    {
                        if (GUILayout.Button(btn.label))
                            btn.method.Invoke(target, null);
                    }
                }
            }
        }

        // ── Inspectable field rendering ────────────────────────────────────────

        // Foldouts stay interactive; only leaf values are drawn disabled.

        private const int MaxNestingDepth = 6;

        private void DrawReflectedField(FieldInfo field, string countFrom)
        {
            DrawMember(field.Name, field.FieldType, field.GetValue(target), target,
                       inspectable: true, countFrom, field.Name, depth: 0);
        }

        private void DrawReflectedProperty(PropertyInfo property, string countFrom)
        {
            DrawMember(property.Name, property.PropertyType, property.GetValue(target), target,
                       inspectable: true, countFrom, property.Name, depth: 0);
        }

        /// <param name="owner">Object declaring the member; countFrom is resolved against it.</param>
        /// <param name="path">Unique key for foldout state, e.g. "_caches[2].colliders".</param>
        private void DrawMember(string name, Type type, object value, object owner,
                                bool inspectable, string countFrom, string path, int depth)
        {
            var label = new GUIContent(ObjectNames.NicifyVariableName(name));

            if (IsListType(type))
            {
                // [Inspectable] lists must say how many slots are valid; plain nested lists show everything.
                int? count = null;
                if (inspectable)
                {
                    if (!TryResolveCount(owner, countFrom, label, out int resolved)) return;
                    count = resolved;
                }

                DrawList(type, (IList)value, label, path, count, depth);
                return;
            }

            DrawValue(type, value, label, path, depth);
        }

        private void DrawValue(Type type, object value, GUIContent label, string path, int depth)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (value == null)                 { EditorGUILayout.LabelField(label, new GUIContent("null"));   return; }
                if (value is RaycastHit hit)       { DrawHit(label, hit.collider, hit.distance);                  return; }
                if (value is RaycastHit2D hit2D)   { DrawHit(label, hit2D.collider, hit2D.distance);              return; }
                if (TryDrawSimpleValue(type, value, label)) return;
            }

            if (value is IList list)
            {
                DrawList(type, list, label, path, count: null, depth);
                return;
            }

            if (depth < MaxNestingDepth)
            {
                var members = GetNestedMembers(value.GetType());
                if (members.Count > 0)
                {
                    DrawNested(value, members, label, path, depth);
                    return;
                }
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.LabelField(label, new GUIContent(value.ToString()));
        }

        private bool DrawFoldout(Rect rect, string path, GUIContent label)
        {
            var key = (target.GetInstanceID(), path);
            _foldoutState.TryGetValue(key, out bool expanded);
            expanded = EditorGUI.Foldout(rect, expanded, label, true);
            _foldoutState[key] = expanded;
            return expanded;
        }

        // ── Nested struct / class rendering ────────────────────────────────────

        private class NestedMember
        {
            public MemberInfo member;
            public Type type;
            public bool inspectable;
            public string countFrom;

            public object GetValue(object owner) => member is FieldInfo f
                ? f.GetValue(owner)
                : ((PropertyInfo)member).GetValue(owner);
        }

        private static readonly Dictionary<Type, List<NestedMember>> _nestedMembersCache
            = new Dictionary<Type, List<NestedMember>>();

        // Same visibility rules as the top level: serialized fields, plus [Inspectable] fields and properties.
        private static List<NestedMember> GetNestedMembers(Type type)
        {
            if (_nestedMembersCache.TryGetValue(type, out var cached))
                return cached;

            var result = new List<NestedMember>();
            foreach (var member in GetMembersInOrder(type))
            {
                var attr = member.GetCustomAttribute<InspectableAttribute>();
                if (member is FieldInfo field)
                    result.Add(new NestedMember { member = field, type = field.FieldType, inspectable = attr != null, countFrom = attr?.countFrom });
                else if (member is PropertyInfo prop)
                    result.Add(new NestedMember { member = prop, type = prop.PropertyType, inspectable = true, countFrom = attr?.countFrom });
            }

            _nestedMembersCache[type] = result;
            return result;
        }

        private void DrawNested(object value, List<NestedMember> members, GUIContent label, string path, int depth)
        {
            if (!DrawFoldout(EditorGUILayout.GetControlRect(), path, label)) return;

            EditorGUI.indentLevel++;
            foreach (var m in members)
            {
                DrawMember(m.member.Name, m.type, m.GetValue(value), value,
                           m.inspectable, m.countFrom, $"{path}.{m.member.Name}", depth + 1);
            }
            EditorGUI.indentLevel--;
        }

        // ── List rendering ─────────────────────────────────────────────────────

        private static bool IsListType(Type type) => typeof(IList).IsAssignableFrom(type);

        private static Type GetListElementType(Type listType)
        {
            if (listType.IsArray) return listType.GetElementType();
            if (listType.IsGenericType) return listType.GetGenericArguments()[0];
            return typeof(object);
        }

        private static bool TryResolveCount(object owner, string countFrom, GUIContent label, out int count)
        {
            count = 0;

            if (countFrom == null)
            {
                EditorGUILayout.HelpBox($"{label.text}: arrays and lists need [Inspectable(countFrom: ...)].", MessageType.Error);
                return false;
            }

            var cacheKey = (owner.GetType(), countFrom);
            if (!_countFromCache.TryGetValue(cacheKey, out var countDel))
            {
                countDel = BuildIntDelegate(owner.GetType(), countFrom);
                _countFromCache[cacheKey] = countDel;
            }

            if (countDel == null)
            {
                EditorGUILayout.HelpBox($"{label.text}: countFrom \"{countFrom}\" is not an int field, property or zero-arg method.", MessageType.Error);
                return false;
            }

            count = countDel(owner);
            return true;
        }

        /// <param name="count">Number of leading elements to show; null shows all of them.</param>
        private void DrawList(Type listType, IList list, GUIContent label, string path, int? count, int depth)
        {
            if (list == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(label, new GUIContent("null"));
                return;
            }

            int capacity = list.Count;
            int shown = count.HasValue ? Mathf.Clamp(count.Value, 0, capacity) : capacity;

            Rect headerRect = EditorGUILayout.GetControlRect();
            bool expanded = DrawFoldout(headerRect, path, label);
            EditorGUI.LabelField(headerRect, count.HasValue ? $"{shown} / {capacity}" : capacity.ToString(), CountLabelStyle);

            if (!expanded) return;

            Type elementType = GetListElementType(listType);
            EditorGUI.indentLevel++;

            for (int i = 0; i < shown; i++)
            {
                object element = list[i];
                DrawValue(element?.GetType() ?? elementType, element, new GUIContent($"[{i}]"), $"{path}[{i}]", depth + 1);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawHit(GUIContent label, Object collider, float distance)
        {
            const float distanceWidth = 60f;

            Rect rect = EditorGUI.PrefixLabel(EditorGUILayout.GetControlRect(), label);

            // PrefixLabel already applied the indent; stop the fields indenting again.
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var objectRect = new Rect(rect.x, rect.y, rect.width - distanceWidth - 4f, rect.height);
            var distanceRect = new Rect(rect.xMax - distanceWidth, rect.y, distanceWidth, rect.height);
            EditorGUI.ObjectField(objectRect, collider, typeof(Object), true);
            EditorGUI.LabelField(distanceRect, $"d {distance:0.00}", CountLabelStyle);

            EditorGUI.indentLevel = indent;
        }

        private static Func<object, int> BuildIntDelegate(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public
                                     | BindingFlags.Instance  | BindingFlags.DeclaredOnly;

            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(name, flags);
                if (field != null && field.FieldType == typeof(int))
                    return obj => (int)field.GetValue(obj);

                var prop = t.GetProperty(name, flags);
                if (prop != null && prop.CanRead && prop.PropertyType == typeof(int))
                    return obj => (int)prop.GetValue(obj);

                var method = t.GetMethod(name, flags, null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(int))
                    return obj => (int)method.Invoke(obj, null);
            }

            return null;
        }

        private static bool TryDrawSimpleValue(Type type, object value, GUIContent label)
        {
            if (type == typeof(int))    { EditorGUILayout.IntField(label, (int)value);       return true; }
            if (type == typeof(float))  { EditorGUILayout.FloatField(label, (float)value);   return true; }
            if (type == typeof(double)) { EditorGUILayout.DoubleField(label, (double)value); return true; }
            if (type == typeof(bool))   { EditorGUILayout.Toggle(label, (bool)value);        return true; }
            if (type == typeof(string)) { EditorGUILayout.TextField(label, (string)value);   return true; }

            if (type == typeof(Vector2))    { EditorGUILayout.Vector2Field(label, (Vector2)value);       return true; }
            if (type == typeof(Vector3))    { EditorGUILayout.Vector3Field(label, (Vector3)value);       return true; }
            if (type == typeof(Vector4))    { EditorGUILayout.Vector4Field(label, (Vector4)value);       return true; }
            if (type == typeof(Vector2Int)) { EditorGUILayout.Vector2IntField(label, (Vector2Int)value); return true; }
            if (type == typeof(Vector3Int)) { EditorGUILayout.Vector3IntField(label, (Vector3Int)value); return true; }
            if (type == typeof(Color))      { EditorGUILayout.ColorField(label, (Color)value);           return true; }
            if (type == typeof(Bounds))     { EditorGUILayout.BoundsField(label, (Bounds)value);         return true; }
            if (type == typeof(Rect))       { EditorGUILayout.RectField(label, (Rect)value);             return true; }

            if (type == typeof(Quaternion))
            {
                var q = (Quaternion)value;
                EditorGUILayout.Vector4Field(label, new Vector4(q.x, q.y, q.z, q.w));
                return true;
            }

            if (type.IsEnum)
            {
                EditorGUILayout.EnumPopup(label, (Enum)value);
                return true;
            }

            if (typeof(Object).IsAssignableFrom(type))
            {
                EditorGUILayout.ObjectField(label, (Object)value, type, true);
                return true;
            }

            return false;
        }

        // ── ShowIf evaluation ──────────────────────────────────────────────────

        private static bool EvaluateShowIf(string conditionName, object compareValue, Object targetObj, SerializedObject so)
        {
            // Fast path: serialized property.
            var conditionProp = so.FindProperty(conditionName);
            if (conditionProp != null)
            {
                if (compareValue == null)
                    return conditionProp.boolValue;

                switch (conditionProp.propertyType)
                {
                    case SerializedPropertyType.Boolean: return conditionProp.boolValue.Equals(compareValue);
                    case SerializedPropertyType.Enum:    return conditionProp.enumValueIndex.Equals((int)compareValue);
                    case SerializedPropertyType.Integer: return conditionProp.intValue.Equals((int)compareValue);
                    case SerializedPropertyType.Float:   return conditionProp.floatValue.Equals((float)compareValue);
                    case SerializedPropertyType.String:  return conditionProp.stringValue.Equals((string)compareValue);
                }
            }

            // Reflection fallback: non-serialized fields, properties, zero-arg bool methods.
            var key = (targetObj.GetType(), conditionName);
            if (!_showIfCache.TryGetValue(key, out var del))
            {
                del = BuildBoolDelegate(targetObj.GetType(), conditionName);
                _showIfCache[key] = del;
            }

            if (del == null) return true;

            bool raw = del(targetObj);
            return compareValue == null ? raw : raw.Equals(compareValue);
        }

        private static Func<object, bool> BuildBoolDelegate(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public
                                     | BindingFlags.Instance  | BindingFlags.DeclaredOnly;

            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                var field = t.GetField(name, flags);
                if (field != null)
                    return obj => (bool)field.GetValue(obj);

                var prop = t.GetProperty(name, flags);
                if (prop != null && prop.CanRead && prop.PropertyType == typeof(bool))
                    return obj => (bool)prop.GetValue(obj);

                var method = t.GetMethod(name, flags);
                if (method != null && method.GetParameters().Length == 0 && method.ReturnType == typeof(bool))
                    return obj => (bool)method.Invoke(obj, null);
            }

            return null;
        }

        // ── Member scanning ────────────────────────────────────────────────────

        private static (List<Segment> segments, List<ButtonRow> top, List<ButtonRow> bottom) BuildOrFetchAll(Type type)
        {
            if (_typeCache.TryGetValue(type, out var cached))
                return cached;

            var members = GetMembersInOrder(type);
            var segments = new List<Segment>();
            var topRows = new List<ButtonRow>();
            var bottomRows = new List<ButtonRow>();
            var groupsByTitle = new Dictionary<string, GroupedSegment>();
            int groupIndex = 0;

            foreach (var member in members)
            {
                if (member is FieldInfo field)
                {
                    var foldAttr = field.GetCustomAttribute<FoldAttribute>();
                    var inspectableAttr = field.GetCustomAttribute<InspectableAttribute>();

                    SegmentItem item = inspectableAttr != null
                        ? (SegmentItem)new ReflectedFieldItem { field = field, countFrom = inspectableAttr.countFrom }
                        : (SegmentItem)new FieldItem { path = field.Name };

                    if (foldAttr != null)
                    {
                        var group = GetOrCreateGroup(foldAttr.title, foldAttr.titleFrom,
                                                     groupsByTitle, segments, ref groupIndex);
                        group.items.Add(item);
                    }
                    else
                    {
                        if (segments.Count == 0 || !(segments[segments.Count - 1] is UngroupedSegment))
                            segments.Add(new UngroupedSegment());

                        ((UngroupedSegment)segments[segments.Count - 1]).items.Add(item);
                    }
                }
                else if (member is PropertyInfo prop)
                {
                    var foldAttr = prop.GetCustomAttribute<FoldAttribute>();
                    var inspectableAttr = prop.GetCustomAttribute<InspectableAttribute>();
                    SegmentItem item = new ReflectedPropertyItem { property = prop, countFrom = inspectableAttr?.countFrom };

                    if (foldAttr != null)
                    {
                        var group = GetOrCreateGroup(foldAttr.title, foldAttr.titleFrom,
                                                     groupsByTitle, segments, ref groupIndex);
                        group.items.Add(item);
                    }
                    else
                    {
                        if (segments.Count == 0 || !(segments[segments.Count - 1] is UngroupedSegment))
                            segments.Add(new UngroupedSegment());

                        ((UngroupedSegment)segments[segments.Count - 1]).items.Add(item);
                    }
                }
                else if (member is MethodInfo method)
                {
                    var attr = method.GetCustomAttribute<EditorButtonAttribute>();
                    string label = attr.label ?? ObjectNames.NicifyVariableName(method.Name);
                    var info = new ButtonInfo { method = method, label = label };

                    if (attr.fold != null)
                    {
                        var group = GetOrCreateGroup(attr.fold, titleFrom: null,
                                                     groupsByTitle, segments, ref groupIndex);
                        AddButtonToGroup(group, info, attr.group);
                    }
                    else
                    {
                        AddButtonToList(attr.position == ButtonPosition.Top ? topRows : bottomRows, info, attr.group);
                    }
                }
            }

            var result = (segments, topRows, bottomRows);
            _typeCache[type] = result;
            return result;
        }

        private static GroupedSegment GetOrCreateGroup(
            string title, string titleFrom,
            Dictionary<string, GroupedSegment> groupsByTitle,
            List<Segment> segments, ref int groupIndex)
        {
            if (!groupsByTitle.TryGetValue(title, out var group))
            {
                group = new GroupedSegment
                {
                    title = title,
                    titleFrom = titleFrom,
                    groupIndex = groupIndex++
                };
                groupsByTitle[title] = group;
                segments.Add(group);
            }
            return group;
        }

        private static void AddButtonToGroup(GroupedSegment group, ButtonInfo info, string buttonGroup)
        {
            var lastItem = group.items.Count > 0 ? group.items[group.items.Count - 1] : null;
            if (buttonGroup != null && lastItem is ButtonRowItem lastBri && lastBri.group == buttonGroup)
            {
                lastBri.row.buttons.Add(info);
            }
            else
            {
                var rowItem = new ButtonRowItem { group = buttonGroup };
                rowItem.row.buttons.Add(info);
                group.items.Add(rowItem);
            }
        }

        private static void AddButtonToList(List<ButtonRow> list, ButtonInfo info, string buttonGroup)
        {
            var last = list.Count > 0 ? list[list.Count - 1] : null;
            if (buttonGroup != null && last != null
                && last.buttons[0].method.GetCustomAttribute<EditorButtonAttribute>()?.group == buttonGroup)
            {
                last.buttons.Add(info);
            }
            else
            {
                var row = new ButtonRow();
                row.buttons.Add(info);
                list.Add(row);
            }
        }

        private static List<MemberInfo> GetMembersInOrder(Type type)
        {
            var typeChain = new Stack<Type>();
            for (Type t = type; t != null && t != typeof(MonoBehaviour) && t != typeof(ScriptableObject)
                                          && t != typeof(Behaviour) && t != typeof(Component)
                                          && t != typeof(Object) && t != typeof(object); t = t.BaseType)
            {
                typeChain.Push(t);
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public
                                     | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            var result = new List<MemberInfo>();
            while (typeChain.Count > 0)
            {
                var t = typeChain.Pop();
                var members = new List<MemberInfo>();

                foreach (var field in t.GetFields(flags))
                {
                    if (field.IsStatic || field.IsLiteral) continue;

                    bool isSerializable = (field.IsPublic && field.GetCustomAttribute<NonSerializedAttribute>() == null)
                                      || (!field.IsPublic && field.GetCustomAttribute<SerializeField>() != null);
                    bool isInspectable = field.GetCustomAttribute<InspectableAttribute>() != null;

                    if (isSerializable || (isInspectable && !isSerializable))
                        members.Add(field);
                }

                foreach (var prop in t.GetProperties(flags))
                {
                    if (prop.CanRead && prop.GetCustomAttribute<InspectableAttribute>() != null)
                        members.Add(prop);
                }

                foreach (var method in t.GetMethods(flags))
                {
                    if (method.GetCustomAttribute<EditorButtonAttribute>() != null && method.GetParameters().Length == 0)
                        members.Add(method);
                }

                members.Sort((a, b) => a.MetadataToken.CompareTo(b.MetadataToken));
                result.AddRange(members);
            }

            return result;
        }

        private static bool HasAnyReflectedField(List<Segment> segments)
        {
            foreach (var seg in segments)
            {
                var items = seg is UngroupedSegment u ? u.items
                          : seg is GroupedSegment g   ? g.items
                          : null;
                if (items == null) continue;
                foreach (var item in items)
                    if (item is ReflectedFieldItem || item is ReflectedPropertyItem) return true;
            }
            return false;
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
