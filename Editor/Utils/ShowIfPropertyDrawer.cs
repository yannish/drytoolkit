using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ShowIfPropertyDrawer : PropertyDrawer
{
    // Keyed by (target type, condition member name). Null value = searched and not found.
    private static readonly Dictionary<(Type, string), Func<object, bool>> _memberCache
        = new Dictionary<(Type, string), Func<object, bool>>();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!ShouldShow(property))
            return 0;

        return EditorGUI.GetPropertyHeight(property, label);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (ShouldShow(property))
        {
            using (new EditorGUI.IndentLevelScope())
                EditorGUI.PropertyField(position, property, label, true);
        }
    }

    private bool ShouldShow(SerializedProperty property)
    {
        ShowIfAttribute showIf = (ShowIfAttribute)attribute;
        SerializedObject serializedObject = property.serializedObject;

        // --- Path 1: SerializedProperty (serialized fields, fast) ---
        SerializedProperty conditionProp = serializedObject.FindProperty(showIf.conditionField);
        if (conditionProp != null)
        {
            if (showIf.compareValue == null)
                return conditionProp.boolValue;

            switch (conditionProp.propertyType)
            {
                case SerializedPropertyType.Boolean: return conditionProp.boolValue.Equals(showIf.compareValue);
                case SerializedPropertyType.Enum:    return conditionProp.enumValueIndex.Equals((int)showIf.compareValue);
                case SerializedPropertyType.Integer: return conditionProp.intValue.Equals((int)showIf.compareValue);
                case SerializedPropertyType.Float:   return conditionProp.floatValue.Equals((float)showIf.compareValue);
                case SerializedPropertyType.String:  return conditionProp.stringValue.Equals((string)showIf.compareValue);
            }
        }

        // --- Path 2: Reflection fallback (non-serialized fields, properties, methods) ---
        object target = serializedObject.targetObject;
        Type targetType = target.GetType();
        var cacheKey = (targetType, showIf.conditionField);

        if (!_memberCache.TryGetValue(cacheKey, out Func<object, bool> cachedDelegate))
        {
            cachedDelegate = FindRawBoolDelegate(targetType, showIf.conditionField);
            _memberCache[cacheKey] = cachedDelegate;
        }

        if (cachedDelegate == null)
            return true;

        bool rawValue = cachedDelegate(target);
        return showIf.compareValue == null ? rawValue : rawValue.Equals(showIf.compareValue);
    }

    // Walks the type hierarchy looking for a bool-returning field, property, or method.
    // Returns a cached extractor lambda, or null if nothing is found.
    private static Func<object, bool> FindRawBoolDelegate(Type type, string name)
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public
                                 | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        for (Type current = type; current != null && current != typeof(object); current = current.BaseType)
        {
            FieldInfo field = current.GetField(name, flags);
            if (field != null)
            {
                FieldInfo f = field;
                return obj => (bool)f.GetValue(obj);
            }

            PropertyInfo prop = current.GetProperty(name, flags);
            if (prop != null && prop.CanRead && prop.PropertyType == typeof(bool))
            {
                PropertyInfo p = prop;
                return obj => (bool)p.GetValue(obj);
            }

            MethodInfo method = current.GetMethod(name, flags);
            if (method != null && method.GetParameters().Length == 0 && method.ReturnType == typeof(bool))
            {
                MethodInfo m = method;
                return obj => (bool)m.Invoke(obj, null);
            }
        }

        return null;
    }
}
