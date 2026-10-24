using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

// Shared condition evaluation for ShowIf / EnableIf drawers.
public static class ConditionalAttributeUtils
{
    // Keyed by (target type, condition member name). Null value = searched and not found.
    private static readonly Dictionary<(Type, string), Func<object, bool>> _memberCache
        = new Dictionary<(Type, string), Func<object, bool>>();

    // Returns true if the condition is met, or if the condition member can't be found.
    public static bool Evaluate(SerializedProperty property, string conditionField, object compareValue)
    {
        SerializedObject serializedObject = property.serializedObject;

        // --- Path 1: SerializedProperty (serialized fields, fast) ---
        SerializedProperty conditionProp = serializedObject.FindProperty(conditionField);
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

        // --- Path 2: Reflection fallback (non-serialized fields, properties, methods) ---
        object target = serializedObject.targetObject;
        Type targetType = target.GetType();
        var cacheKey = (targetType, conditionField);

        if (!_memberCache.TryGetValue(cacheKey, out Func<object, bool> cachedDelegate))
        {
            cachedDelegate = FindRawBoolDelegate(targetType, conditionField);
            _memberCache[cacheKey] = cachedDelegate;
        }

        if (cachedDelegate == null)
            return true;

        bool rawValue = cachedDelegate(target);
        return compareValue == null ? rawValue : rawValue.Equals(compareValue);
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
